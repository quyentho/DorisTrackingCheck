import "./styles.css";
import {
  exportCsv,
  getData,
  jsonToCsv,
  getDataIMile,
  getDataNaqel,
} from "./utils";

const textbox = document.getElementById("textbox")! as HTMLInputElement;
const form = document.getElementById("form")!;
const btnSubmit = document.getElementById("btn-submit")! as HTMLButtonElement;
const dropdown = document.getElementById("export-option")! as HTMLSelectElement;

const spinners = document.getElementsByClassName(
  "spinner-border"
)! as HTMLCollectionOf<HTMLElement>;

function displaySpinners() {
  btnSubmit.disabled = true;
  Array.from(spinners).forEach((spinner) => {
    // Do stuff here
    spinner.style.display = "inline-block";
  });
}
function hideSpinners() {
  btnSubmit.disabled = false;
  Array.from(spinners).forEach((spinner) => {
    spinner.style.display = "none";
  });
}
const shipaHandler = async (trackingCodes: string[]) => {
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
};

const imileHandler = async (trackingCodes: string[]) => {
  try {
    const data = await getDataIMile(trackingCodes);

    const csv = jsonToCsv(data);

    exportCsv(csv); // This will download the data file named "tracking.csv".
  } catch (error) {
    console.error(error);
  }
};

const handleSubmit = async (e: Event) => {
  try {
    displaySpinners();
    const trackingCodes = getTrackingCodes();
    switch (dropdown.value) {
      case "shipa":
        await shipaHandler(trackingCodes);
        break;
      case "imile":
        await imileHandler(trackingCodes);
        break;
      case "naqel":
        const response = await getDataNaqel(trackingCodes);
        console.log("response", response);

        const href = URL.createObjectURL(response.data);

        // create "a" HTML element with href to file & click
        const link = document.createElement("a");
        link.href = href;
        link.setAttribute("download", "tracking.csv");
        document.body.appendChild(link);
        link.click();

        // clean up "a" element & remove ObjectURL
        document.body.removeChild(link);
        URL.revokeObjectURL(href);

        break;
      default:
        break;
    }
  } catch (error) {
    console.error(error);
  } finally {
    hideSpinners();
  }
};

btnSubmit.addEventListener("click", handleSubmit);

if ("serviceWorker" in navigator) {
  navigator.serviceWorker.register("./sw.js").then(function () {
    console.log("Service Worker Registered");
  });
}

window.addEventListener("beforeinstallprompt", (e) => {});

function getTrackingCodes() {
  return textbox.value
    .split(/\r?\n/)
    .map((v) => v.trim())
    .filter(Boolean);
}
