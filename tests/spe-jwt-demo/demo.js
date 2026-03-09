const https = require("https");
var HttpsProxyAgent = require('https-proxy-agent');
const tokener = require('./tokener');

var proxy = process.env.http_proxy || 'http://localhost:8888';
console.log('using proxy server %j', proxy);

const secret = '7AF6F59C14A05786E97012F054D1FB98AC756A2E54E5C9ACBAEE147D9ED0E0DB'
var hostname = 'spe.dev.local';
var username = 'sitecore\\admin'
//var username = 'sitecore\\PowerShellExtensionsAPI';
var token = tokener.getToken('HS256', secret, hostname, 30, username);

const path = '/-/script/v2/master/HomeAndDescendants?offset=3&limit=2&fields=(Name,ItemPath,Id)'
const options = {
    hostname: hostname,
    path: path,
    headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
    },
    rejectUnauthorized: false
}
var agent = new HttpsProxyAgent(proxy);
options.agent = agent;

function handleData(data) {
    console.log(data);
}

https.get(options, (response) => {
    var result = ''
    response.on('data', function (chunk) {
        result += chunk;
    });

    response.on('end', function () {
        handleData(result);
    });
    if(response.statusCode !== 200) {
        console.log(response.statusCode, response.statusMessage);
    }
}).on("error", (err) => {
    console.log("Error: " + err.message);
});