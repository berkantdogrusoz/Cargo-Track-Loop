using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

namespace ColorCargoLoop
{
    /// <summary>
    /// Bir coin paketi: Google Play Console'daki urun ID'si + verilecek coin.
    /// </summary>
    [System.Serializable]
    public class CoinPack
    {
        public string productId;   // Play Console'da ayni ID ile urun ac (orn: coins_500)
        public int coinAmount;     // satin alinca verilecek coin
    }

    /// <summary>
    /// Google Play IAP (Unity Purchasing) yoneticisi.
    /// - Unity Purchasing paketi KURULU ise (UNITY_PURCHASING) gercek satin alma calisir.
    /// - Kurulu DEGILSE stub: Buy() sadece log atar, derleme BOZULMAZ.
    /// Magaza buton onClick -> BillingManager.Buy("coins_500") (Inspector'da product ID yazilir).
    /// </summary>
    public sealed class BillingManager : MonoBehaviour
    {
        public static BillingManager Instance { get; private set; }

        [Tooltip("Coin paketleri: Play Console product ID + verilecek coin. Play'de ayni ID ile CONSUMABLE urun ac.")]
        [SerializeField] private CoinPack[] coinPacks =
        {
            new CoinPack { productId = "coins_500",  coinAmount = 500 },
            new CoinPack { productId = "coins_1200", coinAmount = 1200 },
            new CoinPack { productId = "coins_3000", coinAmount = 3000 },
        };

#if UNITY_PURCHASING
        StoreController storeController;
#endif

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
#if UNITY_PURCHASING
            InitIAP();
#endif
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Magaza buton onClick buraya baglanir (Inspector'da product ID yazilir).</summary>
        public void Buy(string productId)
        {
#if UNITY_PURCHASING
            var product = storeController?.GetProducts()?.FirstOrDefault(p => p.definition.id == productId);
            if (product != null) storeController.PurchaseProduct(product);
            else Debug.LogWarning("[Billing] IAP urunu hazir degil ya da bulunamadi: " + productId);
#else
            Debug.LogWarning("[Billing] Unity Purchasing paketi KURULU DEGIL. Window>Package Manager>In-App Purchasing kur. (" + productId + ")");
#endif
        }

        /// <summary>Ayarlar paneli "Satin Alimlari Geri Yukle" butonu buraya baglanir.</summary>
        public void Restore()
        {
#if UNITY_PURCHASING
            if (storeController != null) { storeController.FetchPurchases(); Debug.Log("[Billing] restore: satin alimlar yeniden cekildi"); }
            else Debug.LogWarning("[Billing] restore: magaza henuz hazir degil");
#else
            Debug.LogWarning("[Billing] Unity Purchasing kurulu degil; restore yok.");
#endif
        }

        void Grant(string productId)
        {
            foreach (var p in coinPacks)
            {
                if (p == null || p.productId != productId) continue;
                var game = FindObjectOfType<ArrowsPixelGame>();
                if (game != null) game.AddCoins(p.coinAmount);
                Analytics.Event("iap_buy_" + productId);
                Altare.Analytics.AltareAnalytics.LogEvent("iap_purchase_success", new Dictionary<string, object> { { "sku", productId }, { "coins", p.coinAmount } });
                return;
            }
        }

#if UNITY_PURCHASING
        async void InitIAP()
        {
            storeController = UnityIAPServices.StoreController();

            storeController.OnStoreConnected += OnStoreConnected;
            storeController.OnStoreDisconnected += failure => Debug.LogWarning("[Billing] store disconnected: " + failure.Message);
            storeController.OnProductsFetched += products => Debug.Log("[Billing] products fetched: " + products.Count);
            storeController.OnProductsFetchFailed += failure => Debug.LogWarning("[Billing] product fetch failed: " + failure.FailureReason);
            storeController.OnPurchasesFetched += orders => Debug.Log("[Billing] purchases fetched");
            storeController.OnPurchasesFetchFailed += failure => Debug.LogWarning("[Billing] purchases fetch failed: " + failure.Message);
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnPurchaseDeferred += order => Debug.Log("[Billing] purchase deferred");

            await storeController.Connect();
        }

        void OnStoreConnected()
        {
            var products = new List<ProductDefinition>();
            foreach (var p in coinPacks)
                if (p != null && !string.IsNullOrEmpty(p.productId))
                    products.Add(new ProductDefinition(p.productId, ProductType.Consumable));

            storeController.FetchProducts(products);
            storeController.FetchPurchases();
        }

        void OnPurchasePending(PendingOrder order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null) Grant(product.definition.id);
            storeController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder:
                    Debug.Log("[Billing] purchase confirmed");
                    break;
                case FailedOrder failed:
                    Debug.LogWarning("[Billing] purchase confirmation failed: " + failed.FailureReason + " " + failed.Details);
                    break;
            }
        }

        void OnPurchaseFailed(FailedOrder failed)
        {
            var productId = failed.CartOrdered.Items().FirstOrDefault()?.Product?.definition.id ?? "?";
            Debug.LogWarning("[Billing] purchase failed: " + productId + " -> " + failed.FailureReason + " " + failed.Details);
        }
#endif
    }
}