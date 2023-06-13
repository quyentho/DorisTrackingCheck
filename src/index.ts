import "./styles.css";
import { exportCsv, getData, jsonToCsv, getDataIMile } from "./utils";

const textbox = document.getElementById("textbox")! as HTMLInputElement;
const form = document.getElementById("form")!;
const btnSubmit = document.getElementById("btn-submit")! as HTMLButtonElement;
const btnSubmitImile = document.getElementById(
  "btn-submit-imile"
)! as HTMLButtonElement;
const spinners = document.getElementsByClassName(
  "spinner-border"
)! as HTMLCollectionOf<HTMLElement>;

function displaySpinners() {
  btnSubmit.disabled = true;
  btnSubmitImile.disabled = true;
  Array.from(spinners).forEach((spinner) => {
    // Do stuff here
    spinner.style.display = "inline-block";
  });
}
function hideSpinners() {
  btnSubmit.disabled = false;
  btnSubmitImile.disabled = false;
  Array.from(spinners).forEach((spinner) => {
    spinner.style.display = "none";
  });
}
const clickHandler = async (e: Event) => {
  e.preventDefault();

  displaySpinners();

  const trackingCodes = textbox.value
    .split(/\r?\n/)
    .map((v) => v.trim())
    .filter(Boolean);

  try {
    console.time("getData");
    const data = await getData(trackingCodes);
    console.timeEnd("getData");

    const csv = jsonToCsv(data);

    exportCsv(csv); // This will download the data file named "tracking.csv".
  } catch (error) {
    console.error(error);
  }

  btnSubmit.disabled = false;
  hideSpinners();
};

const clickHandlerImile = async (e: Event) => {
  // e.preventDefault();
  displaySpinners();
  const trackingCodes = textbox.value
    .split(/\r?\n/)
    .map((v) => v.trim())
    .filter(Boolean);

  try {
    console.time("getDataRxJs");
    const data = await getDataIMile(trackingCodes);
    console.timeEnd("getDataRxJs");

    const csv = jsonToCsv(data);

    exportCsv(csv); // This will download the data file named "tracking.csv".
  } catch (error) {
    console.error(error);
  }

  hideSpinners();
};

btnSubmit.addEventListener("click", clickHandler);
btnSubmitImile.addEventListener("click", clickHandlerImile);

if ("serviceWorker" in navigator) {
  navigator.serviceWorker.register("./sw.js").then(function () {
    console.log("Service Worker Registered");
  });
}

window.addEventListener("beforeinstallprompt", (e) => {});
