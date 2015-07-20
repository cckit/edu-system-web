// -----------------------------------------------------------------------
// <copyright file="KinectWorker-1.8.0.js" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

"use strict";

// Copy the information in the specified image ArrayBuffer to the ImageData associated
// with the specified name and post the ImageData back to UI thread.
//     .processImageData( imageName, imageData )
//
// imageName: Name used to refer to canvas ImageData object to receive data from
//            ArrayBuffer.
// imageBuffer: ArrayBuffer containing image data to copy to canvas ImageData structure.
function processImageData(imageName, imageBuffer) {
    var blob = new Blob([imageBuffer], {type: 'image/png'});
    var url = URL.createObjectURL(blob);

    self.postMessage({ "message": "imageReady", "imageName": imageName, "imageUrl": url });
}

// thread message handler
addEventListener('message', function (event) {
    switch (event.data.message) {
        case "processImageData":
            processImageData(event.data.imageName, event.data.imageBuffer);
            break;
    }
});
