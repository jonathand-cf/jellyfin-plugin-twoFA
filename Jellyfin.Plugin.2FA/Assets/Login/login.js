const serverUrl = "{{SERVER_URL}}";

const form = document.getElementById("twofaLoginForm");
const errorMessage = document.getElementById("errorMessage");

let deviceId;
let deviceName;

window.onload = function () {
    const creds = localStorage.getItem("jellyfin_credentials");
    if (creds) {
        const parsedCreds = JSON.parse(creds);
        if (parsedCreds && parsedCreds.Servers && parsedCreds.Servers.length > 0) {
            const server = parsedCreds.Servers[0];
            if (server.Connect && server.Connect.Expires && new Date(server.Connect.Expires) > new Date()) {
                window.location.replace(serverUrl);
            }
        }
    }
};

form.addEventListener("submit", function (event) {
    event.preventDefault();
    clearError();

    const username = document.getElementById("txtUsername").value.trim();
    const password = document.getElementById("txtPassword").value;
    const otp = document.getElementById("txtOtp").value.trim();

    if (!username || !password) {
        showError("Username and password are required.");
        return;
    }

    if (!deviceName) {
        deviceName = getDeviceName();
    }
    if (!deviceId) {
        deviceId = localStorage.getItem("_deviceId2");
        if (!deviceId) {
            deviceId = generateDeviceId2();
            localStorage.setItem("_deviceId2", deviceId);
        }
    }

    fetch(serverUrl + "/sso/2fa/authenticate", {
        method: "POST",
        body: JSON.stringify({
            Username: username,
            Password: password,
            Otp: otp
        }),
        headers: {
            "Content-Type": "application/json; charset=UTF-8",
            "X-DeviceName": deviceName,
            "X-DeviceId": deviceId
        }
    }).then((response) => response.json())
        .then((data) => {
            if (data.Ok) {
                setCredentialsAndRedirect(data.AuthenticatedUser);
            } else {
                showError(data.ErrorMessage || "Authentication failed.");
            }
        })
        .catch(() => showError("Network error."));
});

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.style.display = "block";
}

function clearError() {
    errorMessage.textContent = "";
    errorMessage.style.display = "none";
}

function setCredentialsAndRedirect(resultData) {
    if (!resultData) {
        showError("Invalid authentication response.");
        return;
    }

    resultData.User.Id = resultData.User.Id.replaceAll("-", "");

    const userKeys = Object.keys(resultData.User);
    userKeys.forEach((element) => {
        if (resultData.User[element] === null || resultData.User[element] === undefined) {
            delete resultData.User[element];
        }
    });

    const userId = `user-${resultData.User.Id}-${resultData.User.ServerId}`;
    localStorage.setItem(userId, JSON.stringify(resultData.User));

    const storedCreds = JSON.parse(localStorage.getItem("jellyfin_credentials") || "{}");
    storedCreds.Servers = storedCreds.Servers || [];
    const currentServer = storedCreds.Servers[0] || {};
    currentServer.UserId = resultData.User.Id;
    currentServer.Id = resultData.User.ServerId;
    currentServer.AccessToken = resultData.AccessToken;
    currentServer.ManualAddress = serverUrl;
    currentServer.manualAddressOnly = true;
    storedCreds.Servers[0] = currentServer;

    localStorage.setItem("jellyfin_credentials", JSON.stringify(storedCreds));
    localStorage.setItem("enableAutoLogin", "true");

    setTimeout(() => {
        window.location.replace(serverUrl);
    }, 200);
}

function generateDeviceId2() {
    return btoa([navigator.userAgent, new Date().toISOString()].join("|")).replace(/=/g, "1");
}

function getDeviceName() {
    function detectBrowser() {
        const userAgent = navigator.userAgent.toLowerCase();
        const browser = {};

        browser.ipad = /ipad/.test(userAgent);
        browser.iphone = /iphone/.test(userAgent);
        browser.android = /android/.test(userAgent);

        browser.tizen = userAgent.includes("tizen") || window.tizen != null;
        browser.web0s = userAgent.includes("netcast") || userAgent.includes("web0s");
        browser.operaTv = userAgent.includes("tv") && userAgent.includes("opr/");
        browser.xboxOne = userAgent.includes("xbox");
        browser.ps4 = userAgent.includes("playstation 4");

        const edgeRegex = /(edg|edge|edga|edgios)[ /]([\w.]+)/.test(userAgent);
        browser.edgeChromium = edgeRegex;
        browser.edge = edgeRegex && !browser.edgeChromium;
        browser.chrome = /chrome/.test(userAgent) && !edgeRegex;
        browser.firefox = /firefox/.test(userAgent);
        browser.opera = /opera/.test(userAgent) || /opr/.test(userAgent);
        browser.safari = !browser.chrome && !browser.edgeChromium && !browser.edge && !browser.opera && userAgent.includes("webkit");

        if (!browser.ipad && navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1) {
            browser.ipad = true;
        }

        return browser;
    }

    const browserName = {
        tizen: "Samsung Smart TV",
        web0s: "LG Smart TV",
        operaTv: "Opera TV",
        xboxOne: "Xbox One",
        ps4: "Sony PS4",
        chrome: "Chrome",
        edgeChromium: "Edge Chromium",
        edge: "Edge",
        firefox: "Firefox",
        opera: "Opera",
        safari: "Safari"
    };

    const browser = detectBrowser();
    let name = "Web Browser - 2FA";

    for (const key in browserName) {
        if (browser[key]) {
            name = browserName[key];
            break;
        }
    }

    return name;
}
