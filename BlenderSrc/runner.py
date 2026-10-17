import bpy, os, json, traceback, io, contextlib, subprocess, time
_R="/Users/alexander/git/argensimia/Argensimia/BlenderSrc/"
_state={"m":None}
try:
    _state["m"]=os.path.getmtime(_R+"job.py")
except Exception: pass
def _tick():
    try:
        m=os.path.getmtime(_R+"job.py")
    except Exception:
        return 1.0
    if m!=_state["m"]:
        _state["m"]=m
        buf=io.StringIO(); g={"__name__":"__job__","R":_R}
        t=time.time()
        try:
            with contextlib.redirect_stdout(buf):
                exec(open(_R+"job.py").read(), g)
            st="ok"
        except Exception:
            st="error\n"+traceback.format_exc()
        open(_R+"job_out.txt","w").write("MTIME %s\nSTATUS %s\nTIME %.1f\n%s"%(m,st,time.time()-t,buf.getvalue()[-20000:]))
    return 1.0
if bpy.app.timers.is_registered(_tick) is False:
    bpy.app.timers.register(_tick, persistent=True)
print("RUNNER ON")
