// Phase 92: prevents the browser page from scrolling/zooming when the user pans
// or pinches inside the Unity WebGL canvas, so touch gestures reach the app
// instead of the page. Not build-verified - this project has no WebGL build
// executed in this environment, only compile-checked as plain JavaScript.
mergeInto(LibraryManager.library, {
    HBE_InstallTouchOverride: function () {
        var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
        if (!canvas) return;

        var preventDefault = function (event) {
            event.preventDefault();
        };

        canvas.addEventListener("touchstart", preventDefault, { passive: false });
        canvas.addEventListener("touchmove", preventDefault, { passive: false });
        canvas.addEventListener("gesturestart", preventDefault, { passive: false });
    }
});
