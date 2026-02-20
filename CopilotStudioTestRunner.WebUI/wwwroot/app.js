// File download helper function
window.downloadFile = function (bytes, filename, mimeType) {
    const blob = new Blob([bytes], { type: mimeType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Trigger a file download from a URL (e.g. an API endpoint returning Content-Disposition: attachment)
window.triggerUrlDownload = function (url) {
    window.location.href = url;
};
