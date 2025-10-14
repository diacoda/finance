// custom-swagger-debug.js
// Adds a login form to Swagger UI and auto-applies JWT, with full debug logging

console.log("custom-swagger-debug.js loaded");

function waitForSwaggerUI() {
    if (window.ui && window.ui.preauthorizeApiKey) {
        console.log("Swagger UI ready");
        setupLoginForm();
    } else {
        setTimeout(waitForSwaggerUI, 100);
    }
}

function setupLoginForm() {
    const loginDiv = document.createElement("div");
    loginDiv.style.margin = "10px 0";
    loginDiv.style.display = "flex";
    loginDiv.style.alignItems = "center";
    loginDiv.style.gap = "5px";

    const username = document.createElement("input");
    username.placeholder = "Username";
    username.style.padding = "4px 6px";

    const password = document.createElement("input");
    password.type = "password";
    password.placeholder = "Password";
    password.style.padding = "4px 6px";

    const button = document.createElement("button");
    button.innerText = "Login";
    button.style.padding = "4px 8px";
    button.style.cursor = "pointer";
    button.style.backgroundColor = "#4f46e5";
    button.style.color = "white";
    button.style.border = "none";
    button.style.borderRadius = "4px";

    button.onclick = async () => {
        try {
            const url = `${window.location.origin}/api/auth/login`;
            console.log("Calling login endpoint:", url);

            const res = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    UserName: username.value,
                    Password: password.value
                })
            });

            console.log("Response status:", res.status);
            console.log("Response headers:", Array.from(res.headers.entries()));

            // Peek at raw text first
            const text = await res.text();
            console.log("Raw response text:", text);

            if (!res.ok) throw new Error(`Login failed with status ${res.status}`);

            // Try to parse JSON
            let data;
            try {
                data = JSON.parse(text);
            } catch (err) {
                console.error("Failed to parse JSON:", err);
                alert("Failed to parse login response as JSON. Check console.");
                return;
            }

            console.log("Parsed JSON:", data);

            if (!data.token) {
                alert("Token not found in response. Check console.");
                return;
            }

            console.log("Logged in, token:", data.token);
            window.ui.preauthorizeApiKey("Bearer", `Bearer ${data.token}`);
        } catch (err) {
            console.error(err);
            alert("Login failed. Check console for details.");
        }
    };

    loginDiv.appendChild(username);
    loginDiv.appendChild(password);
    loginDiv.appendChild(button);

    const container = document.querySelector(".swagger-ui");
    if (container) container.prepend(loginDiv);
}

waitForSwaggerUI();
