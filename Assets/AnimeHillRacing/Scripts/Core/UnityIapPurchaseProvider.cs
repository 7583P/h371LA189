using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace AnimeHillRacing.Core
{
    /// <summary>Unity IAP 5 adapter backed by Google Play Billing on Android.</summary>
    public sealed class UnityIapPurchaseProvider : IStorePurchaseProvider
    {
        private readonly RealMoneyEntitlementService entitlements;
        private readonly Dictionary<string, Product> availableProducts =
            new Dictionary<string, Product>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<StorePurchaseResult>> purchaseCallbacks =
            new Dictionary<string, Action<StorePurchaseResult>>(StringComparer.Ordinal);
        private StoreController storeController;
        private ExternalStoreState state = ExternalStoreState.NotInitialized;
        private string lastError = string.Empty;
        private bool initializationStarted;
        private Action<StoreRestoreResult> restoreCallback;

        public UnityIapPurchaseProvider(GameCatalog catalog, StarterPackConfiguration starterPack)
        {
            entitlements = new RealMoneyEntitlementService(catalog, starterPack);
        }

        private bool IsAvailable => Application.platform == RuntimePlatform.Android;
        public bool IsConfigured => IsAvailable && entitlements.Products.Count > 0;
        public bool IsReady => state == ExternalStoreState.Ready;
        public ExternalStoreState State => state;
        public string LastError => lastError;
        public event Action StateChanged;

        public async void Initialize()
        {
            if (initializationStarted) return;
            initializationStarted = true;
            if (!IsAvailable)
            {
                SetState(ExternalStoreState.Failed,
                    "GOOGLE PLAY BILLING IS ONLY AVAILABLE IN AN ANDROID PLAYER");
                return;
            }
            if (entitlements.Products.Count == 0)
            {
                SetState(ExternalStoreState.Failed, "NO GOOGLE PLAY PRODUCT IDS CONFIGURED");
                return;
            }

            SetState(ExternalStoreState.Initializing, string.Empty);
            Debug.Log("[IAP] Initializing Unity IAP 5 / Google Play Billing");
            try
            {
                storeController = UnityIAPServices.StoreController();
                SubscribeStoreEvents();
                storeController.ProcessPendingOrdersOnPurchasesFetched(true);
                await storeController.Connect();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[IAP] Initialization failed: {exception.Message}");
                SetState(ExternalStoreState.Failed, "GOOGLE PLAY BILLING CONNECTION FAILED");
            }
        }

        public bool TryGetLocalizedPrice(string productId, out string localizedPrice)
        {
            localizedPrice = string.Empty;
            if (!IsProductAvailable(productId)) return false;
            localizedPrice = availableProducts[productId].metadata?.localizedPriceString ?? string.Empty;
            return !string.IsNullOrWhiteSpace(localizedPrice);
        }

        public bool IsProductAvailable(string productId) =>
            IsReady && !string.IsNullOrWhiteSpace(productId) &&
            availableProducts.TryGetValue(productId, out Product product) &&
            product != null && product.availableToPurchase;

        public void Purchase(string productId, Action<StorePurchaseResult> completed)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Failed,
                    "PRODUCT ID NOT CONFIGURED"));
                return;
            }
            if (!entitlements.Contains(productId))
            {
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Failed,
                    "PRODUCT IS NOT MAPPED TO A LOCAL ENTITLEMENT"));
                return;
            }
            if (!IsProductAvailable(productId))
            {
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Failed,
                    IsReady ? "PRODUCT UNAVAILABLE IN GOOGLE PLAY" : "GOOGLE PLAY BILLING NOT READY"));
                return;
            }
            if (purchaseCallbacks.ContainsKey(productId))
            {
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Pending,
                    "PURCHASE ALREADY IN PROGRESS"));
                return;
            }

            purchaseCallbacks.Add(productId, completed);
            Debug.Log($"[IAP] Purchase started: {productId}");
            try
            {
                storeController.PurchaseProduct(availableProducts[productId]);
            }
            catch (Exception exception)
            {
                purchaseCallbacks.Remove(productId);
                Debug.LogWarning($"[IAP] Purchase could not start: {exception.Message}");
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Failed,
                    "PURCHASE COULD NOT START"));
            }
        }

        public void RestorePurchases(Action<StoreRestoreResult> completed)
        {
            if (!IsReady || storeController == null)
            {
                completed?.Invoke(new StoreRestoreResult(false, 0,
                    "GOOGLE PLAY BILLING NOT READY"));
                return;
            }
            if (restoreCallback != null)
            {
                completed?.Invoke(new StoreRestoreResult(false, 0,
                    "RESTORE ALREADY IN PROGRESS"));
                return;
            }

            restoreCallback = completed;
            Debug.Log("[IAP] Restoring permanent purchases from Google Play");
            try
            {
                // On Google Play, restoration is an authoritative purchase query. Unity IAP routes
                // unacknowledged orders to OnPurchasePending and confirmed orders below.
                storeController.FetchPurchases();
            }
            catch (Exception exception)
            {
                restoreCallback = null;
                Debug.LogWarning($"[IAP] Restore failed: {exception.Message}");
                completed?.Invoke(new StoreRestoreResult(false, 0, "RESTORE QUERY FAILED"));
            }
        }

        private void SubscribeStoreEvents()
        {
            storeController.OnStoreConnected += OnStoreConnected;
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        }

        private void OnStoreConnected()
        {
            List<ProductDefinition> definitions = new List<ProductDefinition>();
            for (int i = 0; i < entitlements.Products.Count; i++)
            {
                RealMoneyProductDefinition configured = entitlements.Products[i];
                definitions.Add(new ProductDefinition(configured.ProductId,
                    ToUnityProductType(configured.ProductType)));
            }
            storeController.FetchProducts(definitions);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"[IAP] Store disconnected: {failure?.Message}");
            FailActivePurchases("GOOGLE PLAY BILLING DISCONNECTED");
            SetState(ExternalStoreState.Failed, "GOOGLE PLAY BILLING DISCONNECTED");
        }

        private void OnProductsFetched(List<Product> products)
        {
            availableProducts.Clear();
            for (int i = 0; i < products.Count; i++)
            {
                Product product = products[i];
                string id = product?.definition?.id;
                if (!string.IsNullOrWhiteSpace(id) && product.availableToPurchase)
                    availableProducts[id] = product;
            }
            Debug.Log($"[IAP] Products loaded: {availableProducts.Count}/{entitlements.Products.Count}");
            SetState(ExternalStoreState.Ready, availableProducts.Count > 0
                ? string.Empty : "CONFIGURED PRODUCTS ARE UNAVAILABLE IN GOOGLE PLAY");
            storeController.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[IAP] Product fetch failed: {failure?.FailureReason}");
            SetState(ExternalStoreState.Failed, "GOOGLE PLAY PRODUCT METADATA FAILED TO LOAD");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            bool fulfilled = true;
            bool foundProduct = false;
            foreach (CartItem item in order.CartOrdered.Items())
            {
                string productId = item?.Product?.definition?.id;
                if (string.IsNullOrWhiteSpace(productId)) continue;
                foundProduct = true;
                if (!entitlements.TryFulfillNewPurchase(productId, out string error))
                {
                    fulfilled = false;
                    Debug.LogError($"[IAP] Fulfillment failed for {productId}: {error}");
                    CompletePurchaseCallback(productId, new StorePurchaseResult(
                        StorePurchaseStatus.Failed, error));
                    continue;
                }
                Debug.Log($"[IAP] Purchase fulfilled: {productId}");
                CompletePurchaseCallback(productId, new StorePurchaseResult(
                    StorePurchaseStatus.Succeeded, string.Empty));
            }

            if (!foundProduct || !fulfilled) return;
            // In Unity IAP 5, OnPurchasePending means the store order is paid and waiting for the
            // application to fulfill and acknowledge it. Confirmation happens only after local save.
            storeController.ConfirmPurchase(order);
            StateChanged?.Invoke();
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            StorePurchaseStatus status = order.FailureReason == PurchaseFailureReason.UserCancelled
                ? StorePurchaseStatus.Cancelled : StorePurchaseStatus.Failed;
            Debug.Log(status == StorePurchaseStatus.Cancelled
                ? "[IAP] Purchase cancelled"
                : $"[IAP] Purchase failed: {order.FailureReason}");
            foreach (CartItem item in order.CartOrdered.Items())
            {
                string productId = item?.Product?.definition?.id;
                if (!string.IsNullOrWhiteSpace(productId))
                    CompletePurchaseCallback(productId, new StorePurchaseResult(status,
                        status == StorePurchaseStatus.Cancelled
                            ? "PURCHASE CANCELLED" : "GOOGLE PLAY PURCHASE FAILED"));
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log("[IAP] Purchase deferred by Google Play");
            foreach (CartItem item in order.CartOrdered.Items())
            {
                string productId = item?.Product?.definition?.id;
                if (!string.IsNullOrWhiteSpace(productId))
                    CompletePurchaseCallback(productId, new StorePurchaseResult(
                        StorePurchaseStatus.Pending, "PURCHASE PENDING APPROVAL"));
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning($"[IAP] Store acknowledgement failed: {failed.FailureReason}");
            else
                Debug.Log("[IAP] Purchase acknowledged by Google Play");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            HashSet<string> restored = new HashSet<string>(StringComparer.Ordinal);
            for (int orderIndex = 0; orderIndex < orders.ConfirmedOrders.Count; orderIndex++)
            {
                ConfirmedOrder order = orders.ConfirmedOrders[orderIndex];
                foreach (CartItem item in order.CartOrdered.Items())
                {
                    string productId = item?.Product?.definition?.id;
                    if (string.IsNullOrWhiteSpace(productId) || restored.Contains(productId) ||
                        !entitlements.Contains(productId)) continue;
                    if (entitlements.TryRestorePermanentEntitlement(productId, out string error))
                    {
                        restored.Add(productId);
                        Debug.Log($"[IAP] Ownership restored: {productId}");
                    }
                    else
                        Debug.LogError($"[IAP] Ownership restore failed for {productId}: {error}");
                }
            }

            Action<StoreRestoreResult> completed = restoreCallback;
            restoreCallback = null;
            completed?.Invoke(new StoreRestoreResult(true, restored.Count, string.Empty));
            if (restored.Count > 0) StateChanged?.Invoke();
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            string message = failure?.Message ?? "PURCHASE QUERY FAILED";
            Debug.LogWarning($"[IAP] Purchase query failed: {message}");
            lastError = "GOOGLE PLAY OWNERSHIP QUERY FAILED";
            StateChanged?.Invoke();
            Action<StoreRestoreResult> completed = restoreCallback;
            restoreCallback = null;
            completed?.Invoke(new StoreRestoreResult(false, 0, lastError));
        }

        private void CompletePurchaseCallback(string productId, StorePurchaseResult result)
        {
            if (!purchaseCallbacks.TryGetValue(productId, out Action<StorePurchaseResult> completed)) return;
            purchaseCallbacks.Remove(productId);
            completed?.Invoke(result);
        }

        private void FailActivePurchases(string error)
        {
            Action<StorePurchaseResult>[] callbacks =
                new Action<StorePurchaseResult>[purchaseCallbacks.Count];
            purchaseCallbacks.Values.CopyTo(callbacks, 0);
            purchaseCallbacks.Clear();
            StorePurchaseResult result = new StorePurchaseResult(StorePurchaseStatus.Failed, error);
            for (int i = 0; i < callbacks.Length; i++) callbacks[i]?.Invoke(result);
            Action<StoreRestoreResult> restoring = restoreCallback;
            restoreCallback = null;
            restoring?.Invoke(new StoreRestoreResult(false, 0, error));
        }

        private void SetState(ExternalStoreState next, string error)
        {
            bool changed = state != next || lastError != (error ?? string.Empty);
            state = next;
            lastError = error ?? string.Empty;
            if (changed) StateChanged?.Invoke();
        }

        private static ProductType ToUnityProductType(StoreProductType type)
        {
            if (type == StoreProductType.Consumable) return ProductType.Consumable;
            return type == StoreProductType.Subscription
                ? ProductType.Subscription : ProductType.NonConsumable;
        }
    }
}
