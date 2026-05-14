// Adres API backendu (port z backend/Properties/launchSettings.json → profil „http”).
window.BARBERSHOP_API = "http://localhost:5140";

window.addEventListener('DOMContentLoaded', () => {
    const base = window.BARBERSHOP_API || "http://localhost:5140";

    const loginForm = document.getElementById("loginForm");
    if (loginForm?.addEventListener) {
        loginForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const inputs = loginForm.querySelectorAll("input");
            const data = {
                email: inputs[0].value.trim(),
                password: inputs[1].value
            };

            try {
                const response = await fetch(base + "/api/Auth/login", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(data)
                });

                const result = await response.json();

                if (response.ok) {
                    if (result.token) {
                        localStorage.setItem("authToken", result.token);
                        localStorage.setItem("authUser", JSON.stringify(result));
                    }
                    alert("Zalogowano pomyślnie!");
                    window.location.href = "profile.html";
                } else {
                    alert(result.error || "Błąd logowania.");
                }
            } catch {
                alert("Błąd serwera.");
            }
        });
    }

    const registerForm = document.getElementById("registerForm");
    if (registerForm?.addEventListener) {
        registerForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const inputs = registerForm.querySelectorAll("input");
            const data = {
                fullName: inputs[0].value.trim(),
                email: inputs[1].value.trim(),
                password: inputs[2].value,
                confirmPassword: inputs[3].value
            };

            try {
                const response = await fetch(base + "/api/Auth/register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(data)
                });

                const result = await response.json();

                if (response.ok) {
                    if (result.token) {
                        localStorage.setItem("authToken", result.token);
                        localStorage.setItem("authUser", JSON.stringify({
                            email: result.email,
                            fullName: result.fullName,
                            userId: result.userId
                        }));
                    }

                    alert("Rejestracja udana!");
                    window.location.href = "profile.html";
                } else {
                    let message = "Nie udało się zarejestrować.";

                    if (result.errors) {
                        message = Array.isArray(result.errors)
                            ? result.errors.join("\n")
                            : Object.values(result.errors).flat().join("\n");
                    }

                    alert(message);
                }
            } catch (err) {
                alert("Błąd sieci lub serwera.");
                console.error(err);
            }
        });
    }
});
