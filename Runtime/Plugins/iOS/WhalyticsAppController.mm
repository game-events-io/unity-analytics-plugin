#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

extern "C" {
    typedef void (*RequestTrackingAuthorizationCallback)(int status, const char* idfa);

    void _whalytics_requestTrackingAuthorization(RequestTrackingAuthorizationCallback callback) {
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                NSString *idfaString = @"";
                if (status == ATTrackingManagerAuthorizationStatusAuthorized) {
                    idfaString = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
                }
                
                // Callback to Unity
                if (callback) {
                    callback((int)status, [idfaString UTF8String]);
                }
            }];
        } else {
            // Fallback for older iOS versions
            NSString *idfaString = @"";
            if ([[ASIdentifierManager sharedManager] isAdvertisingTrackingEnabled]) {
                idfaString = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
                if (callback) {
                    callback(3, [idfaString UTF8String]); // 3 = Authorized
                }
            } else {
                if (callback) {
                    callback(0, ""); // 0 = Not Determined / Denied
                }
            }
        }
    }

    const char* _whalytics_getAdvertisingIdentifier() {
        NSString *idfaString = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
        if (idfaString == nil) {
            return strdup("");
        }
        return strdup([idfaString UTF8String]);
    }
}
