package com.altare.candycargo.ads;

import android.app.Activity;
import android.util.Log;

import com.google.android.gms.ads.AdError;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.FullScreenContentCallback;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.MobileAds;
import com.google.android.gms.ads.OnUserEarnedRewardListener;
import com.google.android.gms.ads.initialization.InitializationStatus;
import com.google.android.gms.ads.initialization.OnInitializationCompleteListener;
import com.google.android.gms.ads.interstitial.InterstitialAd;
import com.google.android.gms.ads.interstitial.InterstitialAdLoadCallback;
import com.google.android.gms.ads.rewarded.RewardItem;
import com.google.android.gms.ads.rewarded.RewardedAd;
import com.google.android.gms.ads.rewarded.RewardedAdLoadCallback;
import com.unity3d.player.UnityPlayer;

public final class CandyCargoAds {
    private static final String TAG = "CandyCargoAds";

    private static String receiverName;
    private static String interstitialUnitId;
    private static String rewardedUnitId;
    private static InterstitialAd interstitialAd;
    private static RewardedAd rewardedAd;

    private CandyCargoAds() {
    }

    public static void initialize(String unityReceiverName, String interstitialId, String rewardedId) {
        receiverName = unityReceiverName;
        interstitialUnitId = interstitialId;
        rewardedUnitId = rewardedId;

        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            send("OnAdLog", "activity yok");
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                MobileAds.initialize(activity, new OnInitializationCompleteListener() {
                    @Override
                    public void onInitializationComplete(InitializationStatus status) {
                        send("OnAdsInitialized", "android");
                        loadInterstitial();
                        loadRewarded();
                    }
                });
            }
        });
    }

    public static void loadInterstitial() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null || empty(interstitialUnitId)) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                InterstitialAd.load(activity, interstitialUnitId, request(), new InterstitialAdLoadCallback() {
                    @Override
                    public void onAdLoaded(InterstitialAd ad) {
                        interstitialAd = ad;
                        send("OnAdLog", "interstitial loaded");
                    }

                    @Override
                    public void onAdFailedToLoad(LoadAdError error) {
                        interstitialAd = null;
                        send("OnAdLog", "interstitial load failed: " + error.getMessage());
                    }
                });
            }
        });
    }

    public static void showInterstitial() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            send("OnInterstitialUnavailable", "activity yok");
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (interstitialAd == null) {
                    send("OnInterstitialUnavailable", "not loaded");
                    loadInterstitial();
                    return;
                }

                interstitialAd.setFullScreenContentCallback(new FullScreenContentCallback() {
                    @Override
                    public void onAdDismissedFullScreenContent() {
                        interstitialAd = null;
                        send("OnInterstitialClosed", "closed");
                        loadInterstitial();
                    }

                    @Override
                    public void onAdFailedToShowFullScreenContent(AdError error) {
                        interstitialAd = null;
                        send("OnInterstitialFailed", error.getMessage());
                        loadInterstitial();
                    }
                });
                interstitialAd.show(activity);
            }
        });
    }

    public static void loadRewarded() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null || empty(rewardedUnitId)) return;

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                RewardedAd.load(activity, rewardedUnitId, request(), new RewardedAdLoadCallback() {
                    @Override
                    public void onAdLoaded(RewardedAd ad) {
                        rewardedAd = ad;
                        send("OnAdLog", "rewarded loaded");
                    }

                    @Override
                    public void onAdFailedToLoad(LoadAdError error) {
                        rewardedAd = null;
                        send("OnAdLog", "rewarded load failed: " + error.getMessage());
                    }
                });
            }
        });
    }

    public static void showRewarded() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            send("OnRewardedUnavailable", "activity yok");
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (rewardedAd == null) {
                    send("OnRewardedUnavailable", "not loaded");
                    loadRewarded();
                    return;
                }

                rewardedAd.setFullScreenContentCallback(new FullScreenContentCallback() {
                    @Override
                    public void onAdDismissedFullScreenContent() {
                        rewardedAd = null;
                        send("OnRewardedClosed", "closed");
                        loadRewarded();
                    }

                    @Override
                    public void onAdFailedToShowFullScreenContent(AdError error) {
                        rewardedAd = null;
                        send("OnRewardedFailed", error.getMessage());
                        loadRewarded();
                    }
                });
                rewardedAd.show(activity, new OnUserEarnedRewardListener() {
                    @Override
                    public void onUserEarnedReward(RewardItem rewardItem) {
                        send("OnRewardedEarned", rewardItem.getType() + ":" + rewardItem.getAmount());
                    }
                });
            }
        });
    }

    private static AdRequest request() {
        return new AdRequest.Builder().build();
    }

    private static boolean empty(String value) {
        return value == null || value.length() == 0;
    }

    private static void send(String method, String message) {
        Log.d(TAG, method + " " + message);
        if (empty(receiverName)) return;
        UnityPlayer.UnitySendMessage(receiverName, method, message == null ? "" : message);
    }
}
