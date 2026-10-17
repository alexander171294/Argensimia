import subprocess, os, json
_env=dict(os.environ); _env["PATH"]=_env.get("PATH","")+":"+os.path.expanduser("~/.unity/bin")
def unity(*args, timeout=120):
    r=subprocess.run(["unity","command",*args,"--no-banner","--format","json"],capture_output=True,text=True,timeout=timeout,env=_env)
    return (r.stdout+r.stderr)
