internal sealed class MintegralNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.Mintegral;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Empty;
    }

    public string BuildTailScript()
    {
        return "<script>window.gameClose=function(){window.dispatchEvent(new Event('luna:pause'))};window.gameStart=function(){window.dispatchEvent(new Event('luna:unsafe:resume'))};window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();window.dispatchEvent(new Event('luna:unsafe:pause'));window.dispatchEvent(new Event('luna:start'))});window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(){window.pi&&window.pi.logCta&&window.pi.logCta();window.install&&window.install()}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});window.addEventListener('luna:started',function(){window.gameReady&&window.gameReady()});window.addEventListener('luna:ended',function(){window.gameEnd&&window.gameEnd()});</script>";
    }
}
