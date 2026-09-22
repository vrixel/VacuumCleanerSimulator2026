using UnityEngine;

namespace VCS.Core
{
    /// <summary>
    /// The store page and the feedback address, one per platform (Testers Community report of 2026-09-15: a
    /// "Rate your app" button and an in-app feedback loop). The Play In-App Review API would need the Play Core
    /// package; the store page with its review section is the same door on every platform and needs nothing.
    /// </summary>
    public static class StoreLinks
    {
        public const string AndroidPackage = "com.cosnuau.vacuumcleanersimulator2026";
        public const string AppleId = "6809662067";
        public const string MicrosoftStoreId = "9P9HVRJ09PK0";
        public const string FeedbackAddress = "cosnuau@gmail.com";

        public static string StoreName
        {
            get
            {
#if UNITY_ANDROID
                return "Google Play";
#elif UNITY_IOS
                return "the App Store";
#else
                return "the Microsoft Store";
#endif
            }
        }

        public static string RateUrl
        {
            get
            {
#if UNITY_ANDROID
                return "market://details?id=" + AndroidPackage;
#elif UNITY_IOS
                return "https://apps.apple.com/app/id" + AppleId + "?action=write-review";
#else
                return "https://apps.microsoft.com/detail/" + MicrosoftStoreId;
#endif
            }
        }

        public static string FeedbackUrl =>
            "mailto:" + FeedbackAddress + "?subject=" + System.Uri.EscapeDataString(GameManager.GameName + " " + GameManager.Version + " feedback");

        public static void OpenRate()
        {
            Debug.Log("[VCS] Rate: " + RateUrl);
            Application.OpenURL(RateUrl);
        }

        public static void OpenFeedback()
        {
            Debug.Log("[VCS] Feedback: " + FeedbackUrl);
            Application.OpenURL(FeedbackUrl);
        }
    }
}
