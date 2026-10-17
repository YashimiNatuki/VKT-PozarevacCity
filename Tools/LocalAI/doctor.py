"""Read-only local AI diagnostics; uses Python's standard library only."""
import json
import os
import urllib.error
import urllib.request


def probe(url):
    # Do not send local requests through machine-wide HTTP proxies.
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    try:
        with opener.open(url, timeout=4) as response:
            payload = response.read(32769)
            if len(payload) > 32768:
                return None
            return json.loads(payload)
    except (urllib.error.URLError, TimeoutError, ValueError, OSError):
        return None


def diagnose():
    model = os.environ.get("VKT_OLLAMA_MODEL", "llama3.2:3b")
    tags = probe("http://127.0.0.1:11434/api/tags")
    service = probe("http://127.0.0.1:18741/health")
    installed = bool(isinstance(tags, dict) and isinstance(tags.get("models"), list) and
                     any(isinstance(item, dict) and item.get("name") == model for item in tags["models"]))
    correct = isinstance(service, dict) and service.get("service") == "vkt-local-story"
    reason = service.get("reason", "unknown") if correct else "backend_unavailable" if service is None else "wrong_service"
    # Deliberately report no paths, keys, prompts, machine identifiers or arbitrary upstream text.
    reasons = {"", "unknown", "model_missing", "ollama_unavailable", "loading", "busy", "circuit_open", "backend_unavailable", "wrong_service"}
    return {"ollama_reachable": isinstance(tags, dict), "selected_model_installed": installed,
            "vkt_service_reachable": correct, "generation_available": bool(correct and service.get("available") is True),
            "reason": reason if reason in reasons else "unknown", "authored_dialogue_available": True,
            "changes_made": False}


if __name__ == "__main__":
    result = diagnose()
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result["generation_available"] else 1)
