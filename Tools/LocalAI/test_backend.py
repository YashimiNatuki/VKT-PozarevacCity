import json
import sqlite3
import asyncio
from pathlib import Path
import httpx
from fastapi.testclient import TestClient
from backend import create_app, Cache, StoryText, ContactRequest, next_action, StoryService, GeneratedLine
import pytest

FACTS = dict(contact="mila", intent="briefing", chapter=1, campaign=3, heat=0, nutrition=80, stamina=90, job_active=False)
TEXT = dict(title="Dogovor iz centra", line="Mila te čeka u bloku. Proveri dnevnik i drži se dogovora.", tip="Pogledaj trenutnu ponudu u dnevniku.")

def transport(counter, broken=False):
    def reply(request):
        if request.url.path == "/api/tags":
            return httpx.Response(200, json={"models": [{"name": "llama3.2:3b"}]})
        counter.append(json.loads(request.content))
        if broken:
            return httpx.Response(200, json={"message": {"content": "<script>bad</script>"}})
        return httpx.Response(200, json={"message": {"content": json.dumps({"line": TEXT["line"]})}})
    return httpx.MockTransport(reply)

def client(path, upstream):
    return TestClient(create_app(path, upstream), base_url="http://127.0.0.1")

def test_generation_cache_restart_and_game_authority(tmp_path):
    calls = []
    path = tmp_path/"cache.sqlite3"
    with client(path, transport(calls)) as api:
        first = api.post("/v1/contact", json=FACTS)
        assert first.status_code == 200
        assert first.json()["source"] == "ollama"
        assert first.json()["action"] == "jobs"
        assert "cash" not in first.json()
        assert calls[0]["stream"] is False
        assert "format" in calls[0]
        assert api.post("/v1/contact", json=FACTS).json()["source"] == "cache"
        assert len(calls) == 1
    with client(path, transport(calls)) as api:
        assert api.post("/v1/contact", json=FACTS).json()["source"] == "cache"
        assert len(calls) == 1

def test_invalid_payload_origin_host_and_size_are_rejected(tmp_path):
    calls = []
    with client(tmp_path/"cache.db", transport(calls)) as api:
        assert api.post("/v1/contact", json=dict(FACTS, command="give cash")).status_code == 422
        assert api.post("/v1/contact", json=dict(FACTS, heat=101)).status_code == 422
        assert api.post("/v1/contact", json=dict(FACTS, heat="12")).status_code == 422
        assert api.post("/v1/contact", json=FACTS, headers={"Origin": "https://example.org"}).status_code == 403
        assert api.get("/health", headers={"Host": "example.org"}).status_code == 403
        assert api.post("/v1/contact", content="x"*4097, headers={"content-type": "application/json"}).status_code == 413
        assert not calls

def test_malformed_model_outputs_trigger_fallback_and_circuit(tmp_path):
    calls = []
    with client(tmp_path/"cache.db", transport(calls, broken=True)) as api:
        for heat in [1, 11, 21]:
            result = api.post("/v1/contact", json=dict(FACTS, heat=heat)).json()
            assert result["source"] == "offline"
            assert "<" not in result["line"]
        assert api.post("/v1/contact", json=dict(FACTS, heat=31)).json()["reason"] == "circuit_open"
        assert len(calls) == 3

def test_missing_model_never_downloads_and_keeps_game_advice(tmp_path):
    def absent(request):
        assert request.url.path == "/api/tags"
        return httpx.Response(200, json={"models": []})
    with client(tmp_path/"cache.db", httpx.MockTransport(absent)) as api:
        result = api.post("/v1/contact", json=dict(FACTS, nutrition=5)).json()
        assert result["reason"] == "model_missing"
        assert result["action"] == "meal"

def test_cache_is_bounded_and_corrupt_file_is_preserved(tmp_path):
    cache = Cache(tmp_path/"good.db")
    for i in range(270):
        cache.put(str(i), StoryText(**TEXT))
    assert cache.db.execute("SELECT COUNT(*) FROM story").fetchone()[0] == 256
    cache.db.execute("UPDATE story SET updated=0")
    cache.db.commit()
    assert cache.get("269") is None
    cache.close()
    bad = tmp_path/"bad.db"
    original = b"preserve this invalid cache"
    bad.write_bytes(original)
    cache = Cache(bad)
    assert cache.degraded
    cache.put("ok", StoryText(**TEXT))
    assert bad.read_bytes() == original
    cache.close()

def test_action_priority_is_deterministic():
    def action(**changes):
        return next_action(ContactRequest(**dict(FACTS, **changes)))
    assert action(heat=40, nutrition=0, stamina=0) == "evade"
    assert action(nutrition=0, stamina=0) == "meal"
    assert action(stamina=0) == "rest"
    assert action(campaign=1) == "campaign"
    assert action(job_active=True) == "delivery"

def test_generation_is_single_worker_and_rate_is_bounded(tmp_path):
    async def scenario():
        entered, release = asyncio.Event(), asyncio.Event()
        async def upstream(request):
            if request.url.path == "/api/tags":
                return httpx.Response(200, json={"models": [{"name": "llama3.2:3b"}]})
            entered.set()
            await release.wait()
            return httpx.Response(200, json={"message": {"content": json.dumps({"line": TEXT["line"]})}})
        service = StoryService(tmp_path/"cache.db", httpx.MockTransport(upstream))
        try:
            first = asyncio.create_task(service.reply(ContactRequest(**FACTS)))
            await entered.wait()
            second = await service.reply(ContactRequest(**dict(FACTS, contact="vuk")))
            assert second.reason == "busy"
            release.set()
            assert (await first).source == "ollama"
            for i in range(12):
                service.requests.append(__import__("time").monotonic())
            assert (await service.reply(ContactRequest(**dict(FACTS, contact="luka")))).reason == "rate_limited"
        finally:
            await service.close()
    asyncio.run(scenario())

@pytest.mark.parametrize("line", ["Požarevac, 10.11.2023", "Briefing", "Jedna dugačka rečenica koja govori samo o nepoznatom vremenu danas.", "Brate proveri dnevnik na https://example.org i odmah uzmi posao."])
def test_metadata_and_ungrounded_dialogue_are_rejected(line):
    with pytest.raises(ValueError):
        GeneratedLine(line=line)

def test_total_deadline_releases_worker_and_returns_offline(tmp_path, monkeypatch):
    monkeypatch.setattr("backend.REQUEST_DEADLINE", .02)
    async def slow(request):
        await asyncio.sleep(1)
        return httpx.Response(200, json={"models": []})
    with client(tmp_path/"cache.db", httpx.MockTransport(slow)) as api:
        result = api.post("/v1/contact", json=FACTS).json()
        assert result["reason"] == "deadline"
        assert api.get("/health").json()["busy"] is False
