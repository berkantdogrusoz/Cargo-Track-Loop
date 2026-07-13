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
  const cellsByChar = Object.fromEntries(chars.map(ch => [ch, []]));
  const revealedCount = Object.fromEntries(chars.map(ch => [ch, 0]));
  let slotCount = 5;
  let moves = 0;
  let completedMoves = 0;
  let finished = false;

  level.rows.forEach((row, y) => {
    [...row].forEach((ch, x) => {
      if (ch !== "." && cellsByChar[ch]) cellsByChar[ch].push({ x, y });
    });
  });

  const pandaData = [
    ["Y", 32], ["C", 37], ["W", 34],
    ["O", 34], ["G", 38], ["K", 37],
    ["L", 37], ["T", 34], ["B", 38],
    ["P", 36], ["U", 31], ["N", 29]
  ];

  function makeAudioPool(source, count) {
    return Array.from({ length: count }, () => {
      const audio = new Audio(source);
      audio.preload = "auto";
      return audio;
    });
  }

  const soundPools = {
    jump: makeAudioPool("assets/jump.mp3", 2),
    throw: makeAudioPool("assets/throw.mp3", 6),
    land: makeAudioPool("assets/land.mp3", 3),
    complete: makeAudioPool("assets/complete.mp3", 1)
  };
  const soundCursors = { jump: 0, throw: 0, land: 0, complete: 0 };

  function playSound(name, volume = 1, rate = 1) {
    const pool = soundPools[name];
    if (!pool) return;
    let audio = pool.find(item => item.paused || item.ended);
    if (!audio) {
      audio = pool[soundCursors[name] % pool.length];
      soundCursors[name]++;
      audio.pause();
    }
    try {
      audio.currentTime = 0;
      audio.volume = Math.max(0, Math.min(1, volume));
      audio.playbackRate = rate;
      const promise = audio.play();
      if (promise && typeof promise.catch === "function") promise.catch(() => {});
    } catch (_) {}
  }

  function sleep(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
  }

  function drawPortrait(canvas, complete = false) {
    const ctx = canvas.getContext("2d");
    const rows = level.rows;
    const cell = canvas.width / rows[0].length;
    const visited = Object.fromEntries(chars.map(ch => [ch, 0]));
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = "#fff3ca";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    for (let y = 0; y < rows.length; y++) {
      for (let x = 0; x < rows[y].length; x++) {
        const ch = rows[y][x];
        if (ch === ".") continue;
        const colorIndex = visited[ch]++;
        if (complete || colorIndex < revealedCount[ch]) {
          ctx.fillStyle = colorByChar[ch] || level.palette[0];
          ctx.fillRect(x * cell, y * cell, Math.ceil(cell + .25), Math.ceil(cell + .25));
        }
      }
    }
  }

  function pandaMarkup(color, count) {
    return '<span class="ammo"></span><img class="anubis-img" src="assets/anubis-back.png" alt=""><span class="count">' + count + "</span>";
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
        button.dataset.count = count;
        button.dataset.index = index;
        button.setAttribute("aria-label", "Send Anubis to slot");
        button.innerHTML = pandaMarkup(color, count);
        button.addEventListener("pointerdown", tapPanda, { once: true });
        column.appendChild(button);
      });
      queue.appendChild(column);
    }
  }

  function emptySlot() {
    return [...slots.children].find(slot => !slot.classList.contains("occupied"));
  }

  async function animatePandaToSlot(panda, slot) {
    const stageRect = stage.getBoundingClientRect();
    const from = panda.getBoundingClientRect();
    const to = slot.getBoundingClientRect();
    const flyer = panda.cloneNode(true);
    flyer.className = "moving-panda";
    flyer.style.left = (from.left - stageRect.left) + "px";
    flyer.style.top = (from.top - stageRect.top) + "px";
    flyer.style.width = from.width + "px";
    flyer.style.height = from.height + "px";
    stage.appendChild(flyer);
    panda.remove();

    const dx = to.left + to.width * .5 - (from.left + from.width * .5);
    const dy = to.top + to.height * .35 - (from.top + from.height * .5);
    const arc = stageRect.height * .07;
    playSound("jump", .7, 1.05);
    const animation = flyer.animate([
      { transform: "translate(0,0) rotate(0) scale(1)" },
      { transform: `translate(${dx * .52}px,${dy * .42 - arc}px) rotate(-8deg) scale(1.12)`, offset: .52 },
      { transform: `translate(${dx}px,${dy}px) rotate(0) scale(.84)` }
    ], { duration: 440, easing: "cubic-bezier(.2,.72,.24,1)" });
    try { await animation.finished; } catch (_) {}
    flyer.remove();
    playSound("land", .5, 1.05);
  }

  function mountPandaInSlot(slot, color, count) {
    const slotPanda = document.createElement("div");
    slotPanda.className = "slot-panda";
    slotPanda.style.setProperty("--cube", colorByChar[color]);
    slotPanda.innerHTML = pandaMarkup(color, count);
    slot.appendChild(slotPanda);
    return slotPanda;
  }

  function pulseThrow(slotPanda) {
    slotPanda.classList.remove("throwing");
    void slotPanda.offsetWidth;
    slotPanda.classList.add("throwing");
    setTimeout(() => slotPanda.classList.remove("throwing"), 170);
  }

  function launchCube(source, color, targetCell) {
    const stageRect = stage.getBoundingClientRect();
    const sourceElement = source.querySelector(".ammo") || source;
    const from = sourceElement.getBoundingClientRect();
    const board = portrait.getBoundingClientRect();
    const targetX = board.left + board.width * ((targetCell.x + .5) / level.rows[0].length);
    const targetY = board.top + board.height * ((targetCell.y + .5) / level.rows.length);
    const cube = document.createElement("i");
    cube.className = "flying-cube";
    cube.style.setProperty("--cube", colorByChar[color]);
    cube.style.setProperty("--from-x", ((from.left + from.width * .5 - stageRect.left) / stageRect.width * 100) + "%");
    cube.style.setProperty("--from-y", ((from.top + from.height * .5 - stageRect.top) / stageRect.height * 100) + "%");
    cube.style.setProperty("--to-x", ((targetX - stageRect.left) / stageRect.width * 100) + "%");
    cube.style.setProperty("--to-y", ((targetY - stageRect.top) / stageRect.height * 100) + "%");
    stage.appendChild(cube);
    pulseThrow(source);
    playSound("throw", .28, .94 + Math.random() * .12);
    setTimeout(() => cube.remove(), 850);
  }

  async function pourFromSlot(slotPanda, color, totalCount) {
    const targetCells = cellsByChar[color];
    const startReveal = revealedCount[color];
    const remainingCells = Math.max(0, targetCells.length - startReveal);
    if (remainingCells === 0) {
      await sleep(240);
      return;
    }

    const visualThrows = Math.min(12, Math.max(9, Math.ceil(totalCount / 4)));
    const cellsPerThrow = Math.ceil(remainingCells / visualThrows);
    const label = slotPanda.querySelector(".count");
    const ammo = slotPanda.querySelector(".ammo");

    for (let i = 0; i < visualThrows && !finished; i++) {
      const targetReveal = Math.min(targetCells.length, startReveal + (i + 1) * cellsPerThrow);
      const targetCell = targetCells[Math.max(startReveal, targetReveal - 1)];
      launchCube(slotPanda, color, targetCell);
      const remainingCount = Math.max(0, totalCount - Math.round((i + 1) * totalCount / visualThrows));
      label.textContent = remainingCount;
      if (remainingCount === 0) ammo.style.opacity = "0";
      setTimeout(() => {
        revealedCount[color] = Math.max(revealedCount[color], targetReveal);
        drawPortrait(portrait);
        playSound("land", .18, 1.16);
      }, 500);
      await sleep(125);
    }

    await sleep(590);
    revealedCount[color] = targetCells.length;
    drawPortrait(portrait);
  }

  async function tapPanda(event) {
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
    const totalCount = Number(panda.dataset.count || 1);
    slot.classList.add("occupied");
    slot.style.setProperty("--cube", colorByChar[color]);

    await animatePandaToSlot(panda, slot);
    if (finished) {
      slot.classList.remove("occupied");
      return;
    }

    const slotPanda = mountPandaInSlot(slot, color, totalCount);
    await pourFromSlot(slotPanda, color, totalCount);
    completedMoves++;

    const departure = slotPanda.animate([
      { transform: "translateY(0) scale(1)", opacity: 1 },
      { transform: "translateY(-28%) scale(1.12)", opacity: 1, offset: .45 },
      { transform: "translateY(-42%) scale(.12)", opacity: 0 }
    ], { duration: 280, easing: "ease-in" });
    try { await departure.finished; } catch (_) {}
    slotPanda.remove();
    slot.classList.remove("occupied");
    slot.style.removeProperty("--cube");

    if (!finished && completedMoves >= 7) completeGame();
  }

  function burstCubes(fromElement, color, amount = 10) {
    const targetCells = cellsByChar[color];
    for (let i = 0; i < amount; i++) {
      setTimeout(() => {
        const target = targetCells[Math.floor(Math.random() * targetCells.length)];
        launchCube(fromElement, color, target);
      }, i * 45);
    }
  }

  function giantCube() {
    if (finished) return;
    hand.classList.add("hidden");
    const remaining = chars.filter(ch => revealedCount[ch] < cellsByChar[ch].length);
    const selected = remaining.slice(0, 3);
    if (selected.length === 0) return;
    playSound("jump", .75, .86);
    burstCubes(document.getElementById("fire"), selected[0], 13);
    setTimeout(() => {
      selected.forEach(ch => { revealedCount[ch] = cellsByChar[ch].length; });
      drawPortrait(portrait);
      const completeColors = chars.filter(ch => revealedCount[ch] >= cellsByChar[ch].length).length;
      if (completeColors >= 10) completeGame();
    }, 900);
  }

  function addSlot() {
    if (finished || slotCount >= 6) return;
    hand.classList.add("hidden");
    playSound("land", .55, 1.2);
    slotCount++;
    renderSlots();
  }

  function completeGame() {
    if (finished) return;
    finished = true;
    chars.forEach(ch => { revealedCount[ch] = cellsByChar[ch].length; });
    drawPortrait(portrait, true);
    drawPortrait(endPortrait, true);
    playSound("complete", .72, 1);
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
    playSound("land", .6, .8);
    const first = queue.querySelector(".anubis-unit");
    if (first) first.animate([{ transform: "rotate(-6deg)" }, { transform: "rotate(6deg)" }, { transform: "none" }], { duration: 300 });
  });
  document.getElementById("settings").addEventListener("pointerdown", () => {
    hand.classList.add("hidden");
    playSound("land", .35, 1.25);
  });
  document.getElementById("cta").addEventListener("click", exitAd);
  endCard.addEventListener("click", event => { if (event.target === endCard) exitAd(); });

  renderSlots();
  renderQueue();
  drawPortrait(portrait);
})();