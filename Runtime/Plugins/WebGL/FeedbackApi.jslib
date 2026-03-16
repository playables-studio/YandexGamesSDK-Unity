const feedbackApiLibrary = {
  
  $feedbackApi: {
    isInitialized: false,
    sdk: undefined,
    feedback: undefined,

    throwIfSdkNotInitialized: function() {
      if (!feedbackApi.sdk && typeof yandexGamesPlugin !== 'undefined') {
        feedbackApi.sdk = yandexGamesPlugin.sdk;
        feedbackApi.isInitialized = yandexGamesPlugin.isInitialized;
      }
      
      if (!feedbackApi.sdk || !feedbackApi.feedback) {
        throw new Error('SDK or Feedback API is not initialized. Make sure YandexGamesSDK is initialized first.');
      }
    },

    initializeFeedback: function() {
      return new Promise((resolve, reject) => {
        if (!feedbackApi.sdk) {
          reject(new Error('SDK not initialized'));
          return;
        }

        feedbackApi.feedback = feedbackApi.sdk.feedback;
        resolve();
      });
    },

    // Can Review Methods
    canReview: function(successCallbackPtr, errorCallbackPtr) {
      try {
        feedbackApi.throwIfSdkNotInitialized();
        
        feedbackApi.feedback.canReview().then(function({ value, reason }) {
          if (value) {
            yandexGamesPlugin.sendResponse(
              successCallbackPtr,
              errorCallbackPtr,
              true,
              null
            );
          }
          else {
            yandexGamesPlugin.sendResponse(
              successCallbackPtr,
              errorCallbackPtr,
              false,
              reason
            );
          }
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            false,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          false,
          error instanceof Error ? error.message : "Unknown error checking review availability"
        );
      }
    },

    // Request Review Methods
    requestReview: function(successCallbackPtr, errorCallbackPtr) {
      try {
        feedbackApi.throwIfSdkNotInitialized();
        
        feedbackApi.feedback.requestReview().then(function({ feedbackSent }) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            feedbackSent,
            null
          );
        }).catch(function(error) {
          yandexGamesPlugin.sendResponse(
            successCallbackPtr,
            errorCallbackPtr,
            false,
            error instanceof Error ? error.message : error
          );
        });
      } catch (error) {
        yandexGamesPlugin.sendResponse(
          successCallbackPtr,
          errorCallbackPtr,
          false,
          error instanceof Error ? error.message : "Unknown error requesting review"
        );
      }
    }
  },

  // External C# calls
  FeedbackApi_CanReview: function(successCallbackPtr, errorCallbackPtr) {
    feedbackApi.canReview(successCallbackPtr, errorCallbackPtr);
  },

  FeedbackApi_RequestReview: function(successCallbackPtr, errorCallbackPtr) {
    feedbackApi.requestReview(successCallbackPtr, errorCallbackPtr);
  }
};

autoAddDeps(feedbackApiLibrary, '$feedbackApi');
mergeInto(LibraryManager.library, feedbackApiLibrary); 
