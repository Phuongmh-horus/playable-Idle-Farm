internal static class NetworkMarkupTemplates
{
    public static string BuildMraidPlayableTailScript()
    {
        return "<script>!function(){var started=!1,paused=!1;function isPlayableVisible(){return mraid.isViewable()&&'hidden'!==mraid.getState()}function updateVisibility(){if(started){if(isPlayableVisible()&&paused){window.dispatchEvent(new Event('luna:resume'));paused=!1}else if(!isPlayableVisible()&&!paused){window.dispatchEvent(new Event('luna:pause'));paused=!0}}else if(isPlayableVisible()){window.dispatchEvent(new Event('luna:start'));started=!0}}function onOrientationChange(){}function onAudioVolumeChange(isUnmuted){window.dispatchEvent(new Event(isUnmuted?'luna:unsafe:unmute':'luna:unsafe:mute'))}var onMraidReady=function(){if('undefined'!=typeof mraid){mraid.removeEventListener('ready',onMraidReady);mraid.addEventListener('viewableChange',updateVisibility);mraid.addEventListener('stateChange',updateVisibility);mraid.addEventListener('orientationchange',onOrientationChange);mraid.addEventListener('audioVolumeChange',onAudioVolumeChange);updateVisibility()}else{window.dispatchEvent(new Event('luna:start'))}};window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();if('undefined'!=typeof mraid){'loading'===mraid.getState()?mraid.addEventListener('ready',onMraidReady):onMraidReady()}else{window.dispatchEvent(new Event('luna:start'))}})}();window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(ios,android){window.pi&&window.pi.logCta&&window.pi.logCta();ios=ios||window.$environment.packageConfig.iosLink;android=android||window.$environment.packageConfig.androidLink;var url=/iphone|ipad|ipod|macintosh/i.test(window.navigator.userAgent.toLowerCase())?ios:android;if('undefined'!=typeof mraid&&mraid.open){mraid.open(url)}else if(url){window.open(url,'_blank')}}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});</script>";
    }

    public static string BuildFacebookPlayableTailScript()
    {
        return "<script>window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(){if('undefined'!=typeof FbPlayableAd&&FbPlayableAd.onCTAClick){FbPlayableAd.onCTAClick()}else{var url=window.$environment.packageConfig.iosLink||window.$environment.packageConfig.androidLink;if(url){window.open(url,'_blank')}}}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});window.addEventListener('open_ctalink',function(){if('undefined'!=typeof FbPlayableAd&&FbPlayableAd.onCTAClick){FbPlayableAd.onCTAClick()}});window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();window.dispatchEvent(new Event('luna:start'))});window.addEventListener('luna:ready',function(){window.dispatchEvent(new Event('luna:build'))});</script>";
    }

    public static string BuildTikTokHeadMarkup()
    {
        return "<script src=\"https://sf16-muse-va.ibytedtos.com/obj/union-fe-nc-i18n/playable/sdk/playable-sdk.js\"></script>";
    }

    public static string BuildTikTokTailScript()
    {
        return "<script>window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();window.dispatchEvent(new Event('luna:start'))});window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(){window.pi&&window.pi.logCta&&window.pi.logCta();window.openAppStore&&window.openAppStore()}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});</script>";
    }

    public static string BuildVungleTailScript()
    {
        return "<script>window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();window.dispatchEvent(new Event('luna:start'))});window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(){window.pi&&window.pi.logCta&&window.pi.logCta();if('undefined'!=typeof parent){parent.postMessage('download','*')}else{console.warn('Vungle parent is not defined')}};Luna.Unity.Playable.Trigger=function(eventCode){if('undefined'!=typeof parent){if(6===eventCode){parent.postMessage('complete','*')}else{console.warn(eventCode+' is Not Implemented on this platform')}}else{console.warn('Vungle parent is not defined')}}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});window.addEventListener('luna:ended',function(){if('undefined'!=typeof parent){parent.postMessage('complete','*')}else{console.warn('Vungle parent is not defined')}});window.addEventListener('luna:ready',function(){window.dispatchEvent(new Event('luna:build'))});</script>";
    }
}
