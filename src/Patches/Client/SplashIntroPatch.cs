using BetterAmongUs.Helpers;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class SplashIntroPatch
{
    internal static bool Skip = false;
    internal static bool BetterIntro = false;
    internal static bool IsReallyDoneLoading = false;
    private static GameObject? BetterLogo;

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start))]
    [HarmonyPrefix]
    private static void SplashManager_Start_Prefix(SplashManager __instance)
    {
        // Reset all flags when splash screen starts
        Skip = false;
        BetterIntro = false;
        IsReallyDoneLoading = false;

        // Hide black overlay by moving it out of view
        __instance.logoAnimFinish.transform.Find("BlackOverlay").transform.SetLocalY(100f);
    }

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    [HarmonyPrefix]
    private static bool SplashManager_Update_Prefix(SplashManager __instance)
    {
        // If skip is triggered, just check if loading is done
        if (Skip)
        {
            CheckIfDone(__instance);
            return false;
        }

        // After 1.8 seconds in BAU intro, remove audio to prevent overlap
        if (Time.time - __instance.startTime > 1.8f && BetterIntro)
        {
            UnityEngine.Object.Destroy(__instance.logoAnimFinish.GetComponent<AudioSource>());
        }

        // Allow mouse click to skip splash screen
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (CheckIfDone(__instance, true))
            {
                Skip = true;
                return false;
            }
        }

        // When game data is loaded and minimum time has passed
        if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad && Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange)
        {
            if (!BetterIntro)
            {
                // Start BAU custom intro sequence
                __instance.startTime = Time.time;
                __instance.logoAnimFinish.gameObject.SetActive(false);
                __instance.logoAnimFinish.gameObject.SetActive(true);

                // Replace InnerSloth logo with BAU logo
                GameObject InnerLogo = __instance.logoAnimFinish.transform.Find("LogoRoot/ISLogo").gameObject;
                BetterLogo = UnityEngine.Object.Instantiate(InnerLogo, InnerLogo.transform.parent);
                InnerLogo.DestroyObj();
                BetterLogo.name = "BetterLogo";
                BetterLogo.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("BetterAmongUs.Resources.Images.BetterAmongUs-Logo.png", 150f);

                // Show black overlay
                __instance.logoAnimFinish.transform.Find("BlackOverlay").transform.SetLocalY(0f);

                BetterIntro = true;
                return false;
            }

            // Check if BAU intro has completed
            CheckIfDone(__instance);
        }

        // Return false to prevent original Update from running (we handle everything)
        return false;
    }

    private static bool CheckIfDone(SplashManager __instance, bool isSkip = false)
    {
        // Allow transition if:
        // 1. BAU intro played for 2 seconds OR
        // 2. User skipped and BAU intro was shown
        if ((Time.time - __instance.startTime > 2f && BetterIntro) || (isSkip && BetterIntro))
        {
            IsReallyDoneLoading = true;

            // Allow scene transition to proceed
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
            __instance.loadingObject.SetActive(true);
            return true;
        }

        return false;
    }
}