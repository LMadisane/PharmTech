// notifications.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveMessage", (user, message) => {
    const msg = `${user}: ${message}`;
    console.log(msg);

    const list = document.getElementById("messagesList");
    if (list) {
        const li = document.createElement("li");
        li.textContent = msg;
        list.appendChild(li);
    }
});

connection.start()
    .then(() => console.log("SignalR Connected"))
    .catch(err => console.error(err.toString()));

document.getElementById("sendButton")?.addEventListener("click", () => {
    const user = document.getElementById("userInput")?.value;
    const message = document.getElementById("messageInput")?.value;

    connection.invoke("SendMessage", user, message)
        .catch(err => console.error(err.toString()));
});