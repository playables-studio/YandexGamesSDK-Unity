const yandexGamesPluginLibrary = {
  
  $yandexGamesPlugin: {
    isInitialized: false,
    sdk: undefined,
    isInitializeCalled: false,

    // Helper function for string allocation
    allocateUnmanagedString: function(string) {
      const stringBufferSize = lengthBytesUTF8(string) + 1;
      const stringBufferPtr = _malloc(stringBufferSize);
      stringToUTF8(string, stringBufferPtr, stringBufferSize);
      return stringBufferPtr;
    },

    // Helper function to send response via dynCall
    sendResponse: function(successCallbackPtr, errorCallbackPtr, data, error) {
      const response = {
        status: !error,
        data: data,
        error: error ? (error instanceof Error ? error.message : error) : null
      };

      const jsonPtr = yandexGamesPlugin.allocateUnmanagedString(JSON.stringify(response));
      if (error) {
        dynCall('vi', errorCallbackPtr, [jsonPtr]);
      } else {
        dynCall('vi', successCallbackPtr, [jsonPtr]);
      }
      _free(jsonPtr);
    },

    initialize: function(successCallbackPtr, errorCallbackPtr) {
      if (yandexGamesPlugin.isInitializeCalled) {
        return;
      }
      yandexGamesPlugin.isInitializeCalled = true;

      const sdkScript = document.createElement('script');
      sdkScript.src = yandexGamesPlugin.isProduction() ? '/sdk.js' : 'https://yandex.ru/games/sdk/v2';
      document.head.appendChild(sdkScript);

      sdkScript.onload = function() {
        window.YaGames.init().then(function(sdk) {
          yandexGamesPlugin.sdk = sdk;
          yandexGamesPlugin.isInitialized = true;
          
          // Initialize other modules if they exist
          if (typeof advertisementApi !== 'undefined') {
            advertisementApi.sdk = sdk;
            advertisementApi.isInitialized = true;
          }
        
          // Initialize purchase API
          if (typeof purchaseApi !== 'undefined') {
            purchaseApi.sdk = sdk;
            purchaseApi.initializePayments().catch(function(error) {
              console.error("Failed to initialize payments:", error);
            });
          }
          
          if (typeof authenticationApi !== 'undefined') {
            authenticationApi.sdk = sdk;
            authenticationApi.isInitialized = true;
            
            // Initialize player account data
            sdk.getPlayer({ scopes: false }).then(function(player) {
              authenticationApi.playerAccount = player;
              if (player.getMode() !== 'lite') {
                 authenticationApi.isAuthorized = true;
              }
            }).catch(function(error) {
              console.error("Failed to initialize player account:", error);
            });
          }
          
          if (typeof cloudStorageApi !== 'undefined') {
            cloudStorageApi.sdk = sdk;
            cloudStorageApi.isInitialized = true;
            
            // Initialize player account if not already done by authentication module
            if (!cloudStorageApi.playerAccount) {
              sdk.getPlayer({ scopes: false }).then(function(player) {
                cloudStorageApi.playerAccount = player;
              }).catch(function(error) {
                console.error("Failed to initialize player account for cloud storage:", error);
              });
            }
          }
          if (typeof leaderboardApi !== 'undefined') {
            leaderboardApi.sdk = sdk;
            leaderboardApi.isInitialized = true;
            
            // Initialize leaderboard API
            sdk.getLeaderboards().then(function(leaderboard) {
              leaderboardApi.leaderboard = leaderboard;
            }).catch(function(error) {
              console.error("Failed to initialize leaderboard:", error);
            });
          }
          
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            { initialized: true },
            null
          );
          
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error
          );
        });
      };
      
      sdkScript.onerror = function() {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          "Failed to load Yandex Games SDK script"
        );
      };
    },
    
    throwIfSdkNotInitialized: function() {
      if (!yandexGamesPlugin.isInitialized) {
        throw new Error('SDK is not initialized. Initialize SDK first using YandexGamesSDK.Initialize()');
      }
    },
    
    getEnvironment: function(successCallbackPtr, errorCallbackPtr) {
      try {
        yandexGamesPlugin.throwIfSdkNotInitialized();
        
        const environmentData = {
          app: {
            id: yandexGamesPlugin.sdk.environment.app.id
          },
          browser: {
            lang: yandexGamesPlugin.sdk.environment.browser.lang
          },
          i18n: {
            lang: yandexGamesPlugin.sdk.environment.i18n.lang,
            tld: yandexGamesPlugin.sdk.environment.i18n.tld
          },
          payload: yandexGamesPlugin.sdk.environment.payload
        };
        
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          environmentData,
          null
        );
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error
        );
      }
    },
    
    getServerTime: function(successCallbackPtr, errorCallbackPtr) {
      try {
        const now = new Date().toISOString();
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          now,
          null
        );
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error
        );
      }
    },
    
    setGameplayReady: function(successCallbackPtr, errorCallbackPtr) {
      try {
        yandexGamesPlugin.throwIfSdkNotInitialized();
        yandexGamesPlugin.sdk.features.LoadingAPI.ready();
        
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          true,
          null
        );
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error
        );
      }
    },
    
    setGameplayStart: function(successCallbackPtr, errorCallbackPtr) {
      try {
        yandexGamesPlugin.throwIfSdkNotInitialized();
        // If there's a start method in the future, call it here
        
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          true,
          null
        );
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error
        );
      }
    },
    
    setGameplayStop: function(successCallbackPtr, errorCallbackPtr) {
      try {
        yandexGamesPlugin.throwIfSdkNotInitialized();
        // If there's a stop method in the future, call it here
        
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          true,
          null
        );
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error
        );
      }
    },
    
    getDeviceType: function() {
      try {
        yandexGamesPlugin.throwIfSdkNotInitialized();
        const deviceType = yandexGamesPlugin.sdk.deviceInfo.type;
        
        switch (deviceType) {
          case 'desktop':
            return 0;
          case 'mobile':
            return 1;
          case 'tablet':
            return 2;
          case 'tv':
            return 3;
          default:
            console.error('Unexpected ysdk.deviceInfo response from Yandex. Assuming desktop. deviceType = ' + JSON.stringify(deviceType));
            return 0; // Default to Desktop
        }
      } catch (error) {
        console.error("Error getting device type:", error);
        return 0; // Default to Desktop on error
      }
    },
    
    isRunningOnYandex: function() {
      const hostname = window.location.hostname;
      return hostname.includes('yandex')
          || hostname.includes('playhop')
          || window.document.URL.includes('yandex');
    },

    isProduction: function() {
      const hostname = window.location.hostname;
      return !hostname.includes('localhost')
          && !hostname.includes('127.0.0.1');
    },

    isInitializedGetter: function() {
      return yandexGamesPlugin.isInitialized ? 1 : 0;
    }
  },

  // C# external methods
  YandexGamesPlugin_Initialize: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.initialize(successCallbackPtr, errorCallbackPtr);
  },

  YandexGamesPlugin_GetEnvironment: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.getEnvironment(successCallbackPtr, errorCallbackPtr);
  },

  YandexGamesPlugin_GetServerTime: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.getServerTime(successCallbackPtr, errorCallbackPtr);
  },
  
  YandexGamesPlugin_SetGameplayReady: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.setGameplayReady(successCallbackPtr, errorCallbackPtr);
  },
  
  YandexGamesPlugin_SetGameplayStart: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.setGameplayStart(successCallbackPtr, errorCallbackPtr);
  },
  
  YandexGamesPlugin_SetGameplayStop: function(successCallbackPtr, errorCallbackPtr) {
    yandexGamesPlugin.setGameplayStop(successCallbackPtr, errorCallbackPtr);
  },
  
  YandexGamesPlugin_IsRunningOnYandex: function() {
    return yandexGamesPlugin.isRunningOnYandex() ? 1 : 0;
  },
  
  YandexGamesPlugin_IsInitialized: function() {
    return yandexGamesPlugin.isInitializedGetter();
  },
  
  YandexGamesPlugin_GetDeviceType: function() {
    return yandexGamesPlugin.getDeviceType();
  }
};

autoAddDeps(yandexGamesPluginLibrary, '$yandexGamesPlugin');
mergeInto(LibraryManager.library, yandexGamesPluginLibrary); 