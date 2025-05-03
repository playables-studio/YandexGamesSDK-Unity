const authenticationApiLibrary = {
  
  $authenticationApi: {
    isInitialized: false,
    sdk: undefined,
    playerAccount: undefined,
    isAuthorized: false,

    throwIfSdkNotInitialized: function() {
      if (!authenticationApi.sdk && typeof yandexGamesPlugin !== 'undefined') {
        authenticationApi.sdk = yandexGamesPlugin.sdk;
        authenticationApi.isInitialized = yandexGamesPlugin.isInitialized;
      }
      
      if (!authenticationApi.sdk) {
        throw new Error('SDK is not initialized. Make sure YandexGamesSDK is initialized first.');
      }
    },

    authenticateUser: function(requireSignin, successCallbackPtr, errorCallbackPtr) {
      try {
        if (!authenticationApi.sdk) {
          throw new Error("Yandex Games SDK not initialized");
        }

        authenticationApi.sdk.getPlayer({
          scopes: false,
          signed: true
        })
          .then(function(player) {
            var isAuthorized = player.getMode() !== 'lite';

            if (requireSignin && !isAuthorized) {
              return authenticationApi.sdk.auth.openAuthDialog()
                  .then(function() {
                    return authenticationApi.sdk.getPlayer({
                      scopes: false,
                      signed: true
                    });
                  })
                  .catch(function(error) {
                    if (error.message.includes("auth window opener")) {
                      return player;
                    }
                    throw error;
                  });
            }
            return player;
          })
          .then(function(player) {
            var isAuthorized = player.getMode() !== 'lite';
            var result = {
              id: player.getUniqueID(),
              name: player.getName() || "Guest",
              isAuthorized: isAuthorized,
              avatarUrlSmall: player.getPhoto("small") || "",
              avatarUrlMedium: player.getPhoto("medium") || "",
              avatarUrlLarge: player.getPhoto("large") || "",
              signature: player.signature || ""
            };

            yandexGamesPlugin.sendResponse(
                successCallbackPtr,
                errorCallbackPtr,
                result,
                null
            );
          })
          .catch(function(error) {
            console.error("Authentication error:", error);
            yandexGamesPlugin.sendResponse(
                successCallbackPtr,
                errorCallbackPtr,
                null,
                error.message || "Authentication failed"
            );
          });

      } catch (error) {
        yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error.message || "Initialization error"
        );
      }
    },

    startAuthorizationPolling: function(repeatDelay, successCallbackPtr, errorCallbackPtr) {
      try {
        authenticationApi.throwIfSdkNotInitialized();

        if (authenticationApi.isAuthorized) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            "Already authorized"
          );
          return;
        }

        function authorizationPollingLoop() {
          if (authenticationApi.isAuthorized) {
            yandexGamesPlugin.sendResponse(
              successCallbackPtr,
              errorCallbackPtr,
              "true",
              null
            );
            return;
          }

          authenticationApi.sdk.getPlayer({ scopes: false }).then(function(player) {
            if (player.getMode() !== 'lite') {
              authenticationApi.isAuthorized = true;
              authenticationApi.playerAccount = player;

              yandexGamesPlugin.sendResponse(
                successCallbackPtr,
                errorCallbackPtr,
                "true",
                null
              );
            } else {
              setTimeout(authorizationPollingLoop, repeatDelay);
            }
          });
        }

        authorizationPollingLoop();
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error during polling"
        );
      }
    },
    
    requestProfilePermission: function(successCallbackPtr, errorCallbackPtr) {
      try {
        authenticationApi.throwIfSdkNotInitialized();
        
        if (!authenticationApi.isAuthorized) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            "Needs authorization"
          );
          return;
        }

        authenticationApi.sdk.getPlayer({ scopes: true }).then(function(player) {
          authenticationApi.playerAccount = player;
          
          const hasPermission = player._personalInfo && 
                               player._personalInfo.scopePermissions && 
                               player._personalInfo.scopePermissions.public_name === 'allow';
          
          if (hasPermission) {
            const userProfile = {
              id: player.getUniqueID() || "",
              name: player.getName() || "Guest",
              isAuthorized: true,
              hasPersonalDataPermission: true,
              avatarUrlSmall: player.getPhoto("small") || "",
              avatarUrlMedium: player.getPhoto("medium") || "",
              avatarUrlLarge: player.getPhoto("large") || ""
            };
            
            yandexGamesPlugin.sendResponse(
              successCallbackPtr,
              errorCallbackPtr,
              JSON.stringify(userProfile),
              null
            );
          } else {
            yandexGamesPlugin.sendResponse(
              successCallbackPtr,
              errorCallbackPtr,
              null,
              "User denied permission"
            );
          }
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            null,
            error instanceof Error ? error.message : "Failed to request profile permission"
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          null,
          error instanceof Error ? error.message : "Unknown error requesting profile permission"
        );
      }
    },

    getIsAuthorized: function() {
      try {
        authenticationApi.throwIfSdkNotInitialized();
        return authenticationApi.isAuthorized;
      } catch (error) {
        console.error("Error checking authorization status:", error);
        return false;
      }
    },

    getHasPersonalProfileDataPermission: function() {
      try {
        if (!authenticationApi.playerAccount) {
          return false;
        }
        return authenticationApi.playerAccount._personalInfo && 
               authenticationApi.playerAccount._personalInfo.scopePermissions && 
               authenticationApi.playerAccount._personalInfo.scopePermissions.public_name === 'allow';
      } catch (error) {
        console.error("Error checking profile permission status:", error);
        return false;
      }
    }
  },

  AuthenticationApi_GetPlayerAccountIsAuthorized: function() {
    return authenticationApi.getIsAuthorized();
  },

  AuthenticationApi_GetPlayerAccountHasPersonalProfileDataPermission: function() {
    return authenticationApi.getHasPersonalProfileDataPermission();
  },

  AuthenticationApi_AuthenticateUser: function(requireSignin, successCallbackPtr, errorCallbackPtr) {
    authenticationApi.authenticateUser(requireSignin, successCallbackPtr, errorCallbackPtr);
  },

  AuthenticationApi_StartAuthorizationPolling: function(repeatDelay, successCallbackPtr, errorCallbackPtr) {
    authenticationApi.startAuthorizationPolling(repeatDelay, successCallbackPtr, errorCallbackPtr);
  },
  
  AuthenticationApi_RequestProfilePermission: function(successCallbackPtr, errorCallbackPtr) {
    authenticationApi.requestProfilePermission(successCallbackPtr, errorCallbackPtr);
  }
};

autoAddDeps(authenticationApiLibrary, '$authenticationApi');
mergeInto(LibraryManager.library, authenticationApiLibrary); 
