"""Loopback-only VKT story service. Game rules and rewards stay in Unity."""
from __future__ import annotations
import asyncio
from contextlib import suppress
from contextlib import asynccontextmanager
import hashlib
import json
import os
from pathlib import Path
import re
import sqlite3
import time
from collections import deque
from typing import Literal

import httpx
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel, ConfigDict, Field, field_validator

Contact = Literal["mila", "vuk", "luka"]
Action = Literal["evade", "meal", "rest", "campaign", "delivery", "jobs"]
REQUEST_DEADLINE = 35

class ContactRequest(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True)
    contact: Contact
    intent: Literal["briefing", "advice"]
    chapter: int = Field(ge=1, le=3)
    campaign: int = Field(ge=0, le=3)
    heat: int = Field(ge=0, le=100)
    nutrition: int = Field(ge=0, le=100)
    stamina: int = Field(ge=0, le=100)
    job_active: bool

class StoryText(BaseModel):
    model_config = ConfigDict(extra="forbid")
    title: str = Field(min_length=3, max_length=60)
    line: str = Field(min_length=8, max_length=260)
    tip: str = Field(min_length=5, max_length=160)

    @field_validator("title", "line", "tip")
    @classmethod
    def plain_text(cls, value: str) -> str:
        value = value.strip()
        if not value or any(not c.isprintable() for c in value) or any(x in value.lower() for x in ("<", ">", "`", "http:", "https:", "www.")):
            raise ValueError("Plain game text is required.")
        return value

class ContactResponse(StoryText):
    source: Literal["ollama", "cache", "offline"]
    action: Action
    reason: str
    model: str
    retry_after: int = Field(default=0, ge=0, le=60)

class GeneratedLine(BaseModel):
    """Only dialogue is generative; headings and objectives remain authored."""
    model_config = ConfigDict(extra="forbid")
    line: str = Field(min_length=30, max_length=220)

    @field_validator("line")
    @classmethod
    def dialogue(cls, value: str) -> str:
        value = StoryText.plain_text(value)
        if len(re.findall(r"[^\W\d_]+", value, re.UNICODE)) < 7 or re.search(r"\d", value):
            raise ValueError("A complete dialogue sentence without fabricated numbers is required.")
        if not any(word in value.lower() for word in ("dogovor", "dnevnik", "posao", "posl", "energ", "ekip", "blok", "lik", "potera", "odmor", "obrok", "cilj", "ponud", "uslov")):
            raise ValueError("Dialogue must refer to the supplied game context.")
        return value

def next_action(facts: ContactRequest) -> Action:
    if facts.heat >= 30:
        return "evade"
    if facts.nutrition < 20:
        return "meal"
    if facts.stamina < 30:
        return "rest"
    if facts.campaign < 3:
        return "campaign"
    return "delivery" if facts.job_active else "jobs"

TIPS = {
    "evade": "Potera u igri ima prednost. Završi je pre novog posla.",
    "meal": "Priđi kiosku i proveri obrok u BLOK / LIK meniju.",
    "rest": "Odmori lika. Energija se obnavlja dok ne sprintuješ.",
    "campaign": "Prati označeni glavni ugovor i koristi E na cilju.",
    "delivery": "Aktivna ruta i rok su u DNEVNIK / POSLOVI.",
    "jobs": "DNEVNIK / POSLOVI prikazuje trenutnu ponudu i uslove.",
}

def fallback(facts: ContactRequest, reason: str, model: str) -> ContactResponse:
    names = {"mila": "Mila", "vuk": "Vuk", "luka": "Luka"}
    lines = {
        "mila": "Brate, reč se drži. Proveri cilj, završi dogovor i vrati potvrdu. Centar pamti pouzdane ljude.",
        "vuk": "Nema prečice do imena u bloku. Prvo sredi stanje lika, pa preuzmi sledeći posao.",
        "luka": "Ekipa računa na tebe. Čuvaj energiju i gledaj dnevnik: uspešan dogovor vredi više od prazne priče.",
    }
    action = next_action(facts)
    return ContactResponse(title=names[facts.contact] + " / veza iz bloka", line=lines[facts.contact],
                           tip=TIPS[action], action=action, source="offline", reason=reason, model=model)

class Cache:
    def __init__(self, path: Path):
        path.parent.mkdir(parents=True, exist_ok=True)
        self.degraded = False
        self.db = sqlite3.connect(path, timeout=2, check_same_thread=False)
        try:
            if self.db.execute("PRAGMA quick_check").fetchone()[0] != "ok":
                raise sqlite3.DatabaseError("Invalid cache")
        except sqlite3.DatabaseError:
            self.db.close()
            self.db = sqlite3.connect(":memory:", check_same_thread=False)
            self.degraded = True
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.execute("CREATE TABLE IF NOT EXISTS story (key TEXT PRIMARY KEY, value TEXT NOT NULL, updated REAL NOT NULL)")
        self.db.commit()

    def get(self, key: str) -> StoryText | None:
        row = self.db.execute("SELECT value FROM story WHERE key=? AND updated>?", (key, time.time()-10800)).fetchone()
        if not row:
            return None
        try:
            return StoryText.model_validate_json(row[0])
        except ValueError:
            return None

    def put(self, key: str, value: StoryText) -> None:
        self.db.execute("INSERT OR REPLACE INTO story VALUES(?,?,?)", (key, value.model_dump_json(), time.time()))
        self.db.execute("DELETE FROM story WHERE key NOT IN (SELECT key FROM story ORDER BY updated DESC LIMIT 256)")
        self.db.commit()

    def close(self):
        self.db.close()

class StoryService:
    def __init__(self, path: Path, transport=None):
        self.model = os.environ.get("VKT_OLLAMA_MODEL", "llama3.2:3b")
        if not re.fullmatch(r"[a-zA-Z0-9_.:/-]{1,80}", self.model) or self.model.endswith(":cloud"):
            raise ValueError("Use an installed local Ollama model.")
        self.client = httpx.AsyncClient(base_url="http://127.0.0.1:11434", timeout=httpx.Timeout(30, connect=2),
                                        transport=transport, trust_env=False, follow_redirects=False)
        self.cache = Cache(path)
        self.lock = asyncio.Lock()
        self.requests = deque()
        self.failures = 0
        self.open_until = 0.0
        self.started = time.monotonic()
        self.metrics = dict(requests=0, generated=0, cache_hits=0, fallbacks=0, busy=0, rate_limited=0)
        self.warm_status = "not_started"
        self.probe_lock = asyncio.Lock()
        self.probe_at = -10.0
        self.probe_reason = "not_checked"

    async def health(self) -> dict:
        # Availability is sampled without loading a model or occupying generation.
        async with self.probe_lock:
            if time.monotonic()-self.probe_at >= 5:
                try:
                    tags = await self.client.get("/api/tags", timeout=2)
                    tags.raise_for_status()
                    names = {m["name"] for m in tags.json()["models"]}
                    self.probe_reason = "" if self.model in names else "model_missing"
                except (httpx.HTTPError, ValueError, KeyError, TypeError):
                    self.probe_reason = "ollama_unavailable"
                self.probe_at = time.monotonic()
        retry = min(60, max(0, int(self.open_until-time.monotonic()+.999)))
        reason = self.probe_reason
        if not reason:
            reason = "circuit_open" if retry else "loading" if self.warm_status == "loading" else "busy" if self.lock.locked() else ""
        return {"service": "vkt-local-story", "status": "ready", "model": self.model,
                "available": not reason, "reason": reason, "retry_after": retry,
                "busy": self.lock.locked(), "warm_status": self.warm_status,
                "cache_degraded": self.cache.degraded,
                "uptime_seconds": int(time.monotonic()-self.started), "metrics": dict(self.metrics)}

    async def warm(self):
        self.warm_status = "loading"
        async with self.lock:
            try:
                async with asyncio.timeout(45):
                    tags = await self.client.get("/api/tags", timeout=2)
                    tags.raise_for_status()
                    if self.model not in {m["name"] for m in tags.json().get("models", [])}:
                        self.warm_status = "model_missing"
                        return
                    result = await self.client.post("/api/generate", json={
                        "model": self.model, "stream": False, "keep_alive": "5m", "options": {"num_ctx": 1024}},
                        timeout=44)
                    result.raise_for_status()
                    self.warm_status = "ready"
            except (TimeoutError, httpx.HTTPError, ValueError, KeyError, TypeError):
                self.warm_status = "unavailable"

    async def close(self):
        await self.client.aclose()
        self.cache.close()

    def offline(self, facts, reason):
        self.metrics["fallbacks"] += 1
        result = fallback(facts, reason, self.model)
        result.retry_after = (min(60, max(1, int(self.open_until-time.monotonic()+.999)))
                              if reason == "circuit_open" else 5 if reason in {"busy", "loading"} else 0)
        if reason == "rate_limited" and self.requests:
            result.retry_after = min(60, max(1, int(self.requests[0]+60-time.monotonic()+.999)))
        return result

    async def reply(self, facts: ContactRequest) -> ContactResponse:
        self.metrics["requests"] += 1
        action = next_action(facts)
        key_data = dict(prompt_version=3, model=self.model, contact=facts.contact, intent=facts.intent, chapter=facts.chapter,
                        campaign=facts.campaign, heat=facts.heat//10, nutrition=facts.nutrition//10,
                        stamina=facts.stamina//10, active=facts.job_active, action=action)
        key = hashlib.sha256(json.dumps(key_data, sort_keys=True).encode()).hexdigest()
        cached = self.cache.get(key)
        if cached:
            self.metrics["cache_hits"] += 1
            return ContactResponse(**cached.model_dump(), action=action, source="cache", reason="", model=self.model)
        now = time.monotonic()
        while self.requests and self.requests[0] <= now-60:
            self.requests.popleft()
        if len(self.requests) >= 12:
            self.metrics["rate_limited"] += 1
            return self.offline(facts, "rate_limited")
        self.requests.append(now)
        if self.open_until > now:
            return self.offline(facts, "circuit_open")
        if self.lock.locked():
            self.metrics["busy"] += 1
            return self.offline(facts, "loading" if self.warm_status == "loading" else "busy")
        async with self.lock:
            try:
                tags = await self.client.get("/api/tags", timeout=2)
                tags.raise_for_status()
                if self.model not in {m["name"] for m in tags.json().get("models", [])}:
                    return self.offline(facts, "model_missing")
                prompt = ("Odgovaraj na srpskom latinicom. Ti si "+facts.contact.capitalize()+
                          ", prijatelj igrača u igri VKT. Napiši jednu kratku rečenicu igraču. "
                          "Vrati JSON sa poljem line.")
                instruction = {
                    "jobs": "Podseti me da proverim dnevnik za sledeći posao u bloku.",
                    "delivery": "Podseti me da proverim dnevnik za trenutni posao i cilj dostave.",
                    "campaign": "Podseti me da pratim označeni cilj glavne misije u igri.",
                    "evade": "Podseti me da je potera u igri važnija od novog posla.",
                    "meal": "Podseti me da kupim obrok na kiosku jer je lik gladan.",
                    "rest": "Podseti me da odmorim lika i sačuvam energiju.",
                }[action]
                result = await self.client.post("/api/chat", json={
                    "model": self.model, "stream": False, "format": GeneratedLine.model_json_schema(),
                    "messages": [{"role": "system", "content": prompt}, {"role": "user", "content": instruction}],
                    "options": {"temperature": .2, "num_predict": 80, "num_ctx": 1024},
                    "keep_alive": "5m",
                })
                result.raise_for_status()
                # Bound even a malformed upstream response before parsing its text.
                if len(result.content) > 32768:
                    raise ValueError("Oversized model response")
                generated = GeneratedLine.model_validate_json(result.json()["message"]["content"])
                authored = fallback(facts, "", self.model)
                story = StoryText(title=authored.title, line=generated.line, tip=TIPS[action])
                self.cache.put(key, story)
                self.failures = 0
                self.metrics["generated"] += 1
                return ContactResponse(**story.model_dump(), source="ollama", action=action, reason="", model=self.model)
            except (httpx.HTTPError, ValueError, KeyError, TypeError, sqlite3.Error) as error:
                self.failures += 1
                if self.failures >= 3:
                    self.open_until = time.monotonic()+60
                reason = ("ollama_unavailable" if isinstance(error, (httpx.ConnectError, httpx.ConnectTimeout)) else
                          "deadline" if isinstance(error, httpx.TimeoutException) else "generation_unavailable")
                return self.offline(facts, reason)

def create_app(cache_path: Path | None = None, ollama_transport=None) -> FastAPI:
    path = cache_path or Path(os.environ.get("LOCALAPPDATA", str(Path.home()/".cache")))/"VKTLocalAI"/"story.sqlite3"
    @asynccontextmanager
    async def lifespan(app):
        app.state.service = StoryService(path, ollama_transport)
        warm_task = asyncio.create_task(app.state.service.warm()) if ollama_transport is None else None
        try:
            yield
        finally:
            if warm_task is not None:
                warm_task.cancel()
                with suppress(asyncio.CancelledError):
                    await warm_task
            await app.state.service.close()
    app = FastAPI(title="VKT Local Story", version="1.0", docs_url=None, redoc_url=None, openapi_url=None, lifespan=lifespan)

    @app.middleware("http")
    async def local_boundary(request: Request, call_next):
        host = request.headers.get("host", "").split(":")[0]
        if host not in {"127.0.0.1", "localhost"} or request.headers.get("origin"):
            return JSONResponse({"error": "local_only"}, status_code=403)
        if request.method == "POST":
            length = request.headers.get("content-length", "")
            if not length.isdecimal() or int(length) > 4096:
                return JSONResponse({"error": "bounded_json_required"}, status_code=413)
            if request.headers.get("content-type", "").split(";")[0] != "application/json":
                return JSONResponse({"error": "json_required"}, status_code=415)
        response = await call_next(request)
        response.headers["Cache-Control"] = "no-store"
        response.headers["X-Content-Type-Options"] = "nosniff"
        return response

    @app.get("/health")
    async def health():
        service = app.state.service
        return await service.health()

    @app.post("/v1/contact", response_model=ContactResponse)
    async def contact(facts: ContactRequest):
        service = app.state.service
        try:
            return await asyncio.wait_for(service.reply(facts), timeout=REQUEST_DEADLINE)
        except TimeoutError:
            service.failures += 1
            if service.failures >= 3:
                service.open_until = time.monotonic()+60
            return service.offline(facts, "deadline")

    return app
