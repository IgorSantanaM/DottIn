window.dottin = window.dottin || {};

window.dottin.downloadFile = async (fileName, contentType, streamReference) => {
    const buffer = await streamReference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    const link = document.createElement('a');

    try {
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
    } finally {
        link.remove();
        // Keep the object URL alive long enough for browsers to start the download.
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    }
};
