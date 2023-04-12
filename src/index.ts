import "./styles.css";
import { exportCsv, getData, jsonToCsv } from "./utils";

const textbox = document.getElementById("textbox")! as HTMLInputElement;
const form = document.getElementById("form")!;
const btnSubmit = document.getElementById("btn-submit")! as HTMLButtonElement;
const spinner = document.getElementsByClassName(
  "spinner-border"
)[0]! as HTMLElement;

const clickHandler = async (e: Event) => {
  e.preventDefault();
  btnSubmit.disabled = true;
  spinner.style.display = "inline-block";
  const trackingCodes = textbox.value
    .split(/\r?\n/)
    .map((v) => v.trim())
    .filter(Boolean);

  try {
    const data = await getData(trackingCodes);

    const csv = jsonToCsv(data);

    exportCsv(csv); // This will download the data file named "tracking.csv".
  } catch (error) {
    console.error(error);
  }

  btnSubmit.disabled = false;
  spinner.style.display = "none";
};

form.addEventListener("submit", clickHandler);
