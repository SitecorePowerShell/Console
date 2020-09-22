const { default: base64url } = require('base64url');
const base64 = require('base64url');
const crypto = require('crypto');

exports.getToken = function(algorithm, secret, authority, validSeconds, name) {
    const validAlgorithms = ['HS256','HS384','HS512'];
    algorithm = (algorithm) ? algorithm.toUpperCase() : algorithm;
    if(!algorithm || !validAlgorithms.includes(algorithm)) {
        console.log('Invalid algorithm. Please use one of the following:', validAlgorithms);
    }

    if(!secret) {
        console.log('Please provide a valid secret.');
    }

    if(!authority) {
        console.log('Please provide a valid authority. Consider providing the hostname to SPE.');
    }

    if(!name) {
        console.log('Please provide a valid name.');
    }

    validSeconds = Math.max(validSeconds, 0);
    if(validSeconds === 0) {
        validSeconds = 30;
    }
    
    const headerObj = {
        alg: algorithm,
        typ: 'JWT'
    };
    
    var nowUtc = new Date();
    nowUtc.setSeconds(nowUtc.getSeconds() + 30);
    var expiration = Math.floor(nowUtc / 1000)
    var hostname = authority.replace(/(^\w+:|^)\/\//, '');
    const payloadObj = {
        iss: 'Web API',
        name: name,
        exp: expiration,
        aud: `https://${hostname}`
    };
       
    const headerObjString = JSON.stringify(headerObj);
    const payloadObjString = JSON.stringify(payloadObj);
    
    const base64UrlHeader = base64(headerObjString);
    const base64UrlPayload = base64(payloadObjString);
    
    var text = base64UrlHeader + "." + base64UrlPayload
    var hashAlgorithm = algorithm.replace('HS', 'sha');
    
    var hmac = crypto.createHmac(hashAlgorithm, secret);
    hmac.update(text);
    var hash = hmac.digest('base64');
    
    var signature = hash.replace(/\+/g, '-').replace(/\//g, '_').split('=')[0];
    var token = text + "." + signature;
    return token;
};