(() => {
  "use strict";

  const stage = document.getElementById("stage");
  const queue = document.getElementById("queue");
  const slots = document.getElementById("slots");
  const hand = document.getElementById("hand");
  const portrait = document.getElementById("portrait");
  const endPortrait = document.getElementById("endPortrait");
  const endCard = document.getElementById("endCard");
  const level = window.PIXEL_POUR_LEVEL;
  const chars = ["P", "B", "Y", "G", "U", "O", "K", "C", "T", "L", "W", "N"];
  const colorByChar = Object.fromEntries(chars.map((ch, index) => [ch, level.palette[index]]));
  const revealed = new Set();
  let slotCount = 5;
  let moves = 0;
  let finished = false;

  const pandaData = [
    ["Y", 32], ["C", 37], ["W", 34],
    ["O", 34], ["G", 38], ["K", 37],
    ["L", 37], ["T", 34], ["B", 38],
    ["P", 36], ["U", 31], ["N", 29]
  ];

  function drawPortrait(canvas, complete = false) {
    const ctx = canvas.getContext("2d");
    const rows = level.rows;
    const cell = canvas.width / rows[0].length;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = "#fff3ca";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    for (let y = 0; y < rows.length; y++) {
      for (let x = 0; x < rows[y].length; x++) {
        const ch = rows[y][x];
        if (ch === ".") continue;
        if (complete || revealed.has(ch)) {
          ctx.fillStyle = colorByChar[ch] || level.palette[0];
          ctx.fillRect(x * cell, y * cell, Math.ceil(cell + .25), Math.ceil(cell + .25));
        }
      }
    }
  }

  function renderSlots() {
    slots.innerHTML = "";
    for (let i = 0; i < slotCount; i++) {
      const slot = document.createElement("div");
      slot.className = "slot";
      slots.appendChild(slot);
    }
  }

  function renderQueue() {
    queue.innerHTML = "";
    for (let columnIndex = 0; columnIndex < 3; columnIndex++) {
      const column = document.createElement("div");
      column.className = "queue-column";
      pandaData.forEach(([color, count], index) => {
        if (index % 3 !== columnIndex) return;
        const button = document.createElement("button");
        button.className = "anubis-unit";
        button.style.setProperty("--cube", colorByChar[color]);
        button.dataset.color = color;
        button.dataset.index = index;
        button.innerHTML = '<span class="ammo"></span><img class="anubis-img" src="assets/anubis.png" alt=""><span class="count">' + count + "</span>";
        button.addEventListener("pointerdown", tapPanda, { once: true });
        column.appendChild(button);
      });
      queue.appendChild(column);
    }
  }
  function emptySlot() {
    return [...slots.children].find(slot => !slot.classList.contains("occupied"));
  }

  function flyCubes(fromElement, color, amount = 7) {
    const stageRect = stage.getBoundingClientRect();
    const from = fromElement.getBoundingClientRect();
    const board = portrait.getBoundingClientRect();
    for (let i = 0; i < amount; i++) {
      const cube = document.createElement("i");
      cube.className = "flying-cube";
      cube.style.setProperty("--cube", colorByChar[color]);
      cube.style.setProperty("--from-x", ((from.left + from.width * .5 - stageRect.left) / stageRect.width * 100) + "%");
      cube.style.setProperty("--from-y", ((from.top + from.height * .2 - stageRect.top) / stageRect.height * 100) + "%");
      cube.style.setProperty("--to-x", ((board.left + board.width * (.28 + Math.random() * .44) - stageRect.left) / stageRect.width * 100) + "%");
      cube.style.setProperty("--to-y", ((board.top + board.height * (.24 + Math.random() * .52) - stageRect.top) / stageRect.height * 100) + "%");
      cube.style.animationDelay = (i * .055) + "s";
      stage.appendChild(cube);
      setTimeout(() => cube.remove(), 1100);
    }
  }

  function revealForMove(primary) {
    revealed.add(primary);
    const next = chars.find(ch => !revealed.has(ch));
    if (next && moves % 2 === 0) revealed.add(next);
    drawPortrait(portrait);
  }

  function tapPanda(event) {
    if (finished) return;
    const panda = event.currentTarget;
    const slot = emptySlot();
    if (!slot) {
      panda.addEventListener("pointerdown", tapPanda, { once: true });
      return;
    }
    hand.classList.add("hidden");
    moves++;
    const color = panda.dataset.color;
    slot.classList.add("occupied");
    slot.style.setProperty("--cube", colorByChar[color]);
    flyCubes(slot, color, 7);
    panda.classList.add("removing");
    setTimeout(() => {
      panda.remove();
      slot.classList.remove("occupied");
      revealForMove(color);
      if (moves >= 7 || queue.querySelectorAll(".anubis-unit").length === 0) completeGame();
    }, 780);
  }

  function giantCube() {
    if (finished) return;
    hand.classList.add("hidden");
    moves++;
    const remaining = chars.filter(ch => !revealed.has(ch));
    remaining.slice(0, 3).forEach(ch => revealed.add(ch));
    const button = document.getElementById("fire");
    flyCubes(button, remaining[0] || "Y", 13);
    setTimeout(() => {
      drawPortrait(portrait);
      if (revealed.size >= 10) completeGame();
    }, 630);
  }

  function addSlot() {
    if (finished || slotCount >= 6) return;
    hand.classList.add("hidden");
    slotCount++;
    renderSlots();
  }

  function completeGame() {
    if (finished) return;
    finished = true;
    chars.forEach(ch => revealed.add(ch));
    drawPortrait(portrait, true);
    drawPortrait(endPortrait, true);
    setTimeout(() => endCard.classList.add("show"), 650);
  }

  function exitAd() {
    if (window.ExitApi && typeof window.ExitApi.exit === "function") {
      window.ExitApi.exit();
      return;
    }
    const url = window.clickTag || "https://play.google.com/store/apps/details?id=com.Altare.CandyCargo";
    window.open(url, "_blank");
  }

  document.getElementById("fire").addEventListener("pointerdown", giantCube);
  document.getElementById("extra").addEventListener("pointerdown", addSlot);
  document.getElementById("hammer").addEventListener("pointerdown", () => {
    hand.classList.add("hidden");
    const first = queue.querySelector(".anubis-unit");
    if (first) first.animate([{ transform: "rotate(-6deg)" }, { transform: "rotate(6deg)" }, { transform: "none" }], { duration: 300 });
  });
  document.getElementById("settings").addEventListener("pointerdown", () => hand.classList.add("hidden"));
  document.getElementById("cta").addEventListener("click", exitAd);
  endCard.addEventListener("click", event => { if (event.target === endCard) exitAd(); });

  renderSlots();
  renderQueue();
  drawPortrait(portrait);
})();