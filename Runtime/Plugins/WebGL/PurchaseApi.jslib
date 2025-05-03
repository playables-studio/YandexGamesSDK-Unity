const purchaseApiLibrary = {
  
  $purchaseApi: {
    isInitialized: false,
    sdk: undefined,
    payments: undefined,

    throwIfSdkNotInitialized: function() {
      if (!purchaseApi.sdk && typeof yandexGamesPlugin !== 'undefined') {
        purchaseApi.sdk = yandexGamesPlugin.sdk;
        purchaseApi.isInitialized = yandexGamesPlugin.isInitialized;
      }
      
      if (!purchaseApi.sdk || !purchaseApi.payments) {
        throw new Error('SDK or Payments API is not initialized. Make sure YandexGamesSDK is initialized first.');
      }
    },

    initializePayments: function() {
      return new Promise((resolve, reject) => {
        if (!purchaseApi.sdk) {
          reject(new Error('SDK not initialized'));
          return;
        }

        purchaseApi.sdk.getPayments({ signed: true })
          .then(function(payments) {
            purchaseApi.payments = payments;
            resolve();
          })
          .catch(reject);
      });
    },

    // Purchase Product Methods
    purchaseProduct: function(productId, developerPayload, successCallbackPtr, errorCallbackPtr) {
      try {
        purchaseApi.throwIfSdkNotInitialized();
        
        purchaseApi.payments.purchase({ 
          id: productId,
          developerPayload: developerPayload || undefined
        }).then(function(purchaseResponse) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            {
              purchasedProduct: purchaseResponse,
              signature: purchaseResponse.signature
            },
            null
          );
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error purchasing product"
        );
      }
    },

    // Consume Product Methods
    consumeProduct: function(purchaseToken, successCallbackPtr, errorCallbackPtr) {
      try {
        purchaseApi.throwIfSdkNotInitialized();
        
        purchaseApi.payments.consumePurchase(purchaseToken).then(function() {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            { consumed: true },
            null
          );
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error consuming product"
        );
      }
    },

    // Get Product Catalog Methods
    getProductCatalog: function(successCallbackPtr, errorCallbackPtr) {
      try {
        purchaseApi.throwIfSdkNotInitialized();
        
        purchaseApi.payments.getCatalog().then(function(products) {
          const formattedProducts = products.map(function(product) {
            return {
              id: product.id,
              title: product.title,
              description: product.description,
              imageURI: product.imageURI,
              price: product.price,
              priceValue: product.priceValue,
              priceCurrencyCode: product.priceCurrencyCode,
              priceCurrencyImage: product.getPriceCurrencyImage('medium')
            };
          });

          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            { products: formattedProducts },
            null
          );
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error getting product catalog"
        );
      }
    },

    // Get Purchased Products Methods
    getPurchasedProducts: function(successCallbackPtr, errorCallbackPtr) {
      try {
        purchaseApi.throwIfSdkNotInitialized();
        
        purchaseApi.payments.getPurchases().then(function(purchases) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            {
              purchasedProducts: purchases,
              signature: purchases.signature
            },
            null
          );
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error getting purchased products"
        );
      }
    }
  },

  // External C# calls
  PurchaseApi_PurchaseProduct: function(productIdPtr, developerPayloadPtr, successCallbackPtr, errorCallbackPtr) {
    const productId = UTF8ToString(productIdPtr);
    const developerPayload = UTF8ToString(developerPayloadPtr);
    purchaseApi.purchaseProduct(productId, developerPayload, successCallbackPtr, errorCallbackPtr);
  },

  PurchaseApi_ConsumeProduct: function(purchaseTokenPtr, successCallbackPtr, errorCallbackPtr) {
    const purchaseToken = UTF8ToString(purchaseTokenPtr);
    purchaseApi.consumeProduct(purchaseToken, successCallbackPtr, errorCallbackPtr);
  },

  PurchaseApi_GetProductCatalog: function(successCallbackPtr, errorCallbackPtr) {
    purchaseApi.getProductCatalog(successCallbackPtr, errorCallbackPtr);
  },

  PurchaseApi_GetPurchasedProducts: function(successCallbackPtr, errorCallbackPtr) {
    purchaseApi.getPurchasedProducts(successCallbackPtr, errorCallbackPtr);
  }
};

autoAddDeps(purchaseApiLibrary, '$purchaseApi');
mergeInto(LibraryManager.library, purchaseApiLibrary); 
