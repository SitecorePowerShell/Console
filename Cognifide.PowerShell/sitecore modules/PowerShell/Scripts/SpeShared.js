function DownloadReport(handle){
      var iframe = document.createElement("iframe");
      iframe.src = '/-/script/handle/'+ handle;
      iframe.width = "1";
      iframe.height = "1";
      iframe.style.position = "absolute";
      iframe.style.display = "none";
      document.body.appendChild(iframe);
}