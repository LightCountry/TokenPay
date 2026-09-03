document.addEventListener("click", async function (event) {
    const button = event.target.closest("[data-copy]");
    if (!button) return;
    const value = button.getAttribute("data-copy");
    if (!value) return;

    const originalTitle = button.title;
    try {
        await navigator.clipboard.writeText(value);
    } catch {
        const input = document.createElement("textarea");
        input.value = value;
        input.style.position = "fixed";
        input.style.opacity = "0";
        document.body.appendChild(input);
        input.select();
        document.execCommand("copy");
        input.remove();
    }
    button.classList.add("copied");
    button.title = "已复制";
    window.setTimeout(() => {
        button.classList.remove("copied");
        button.title = originalTitle;
    }, 1200);
});
