import "./styles.css";
import {
  exportCsv,
  getData,
  jsonToCsv,
  getDataIMile,
  getDataNaqel,
  getDataEu,
} from "./utils";
import { BOXLEO_API_URL } from "./config";

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

const boxleoHandler = async (trackingCodes: string[]) => {
  try {
    // Join tracking codes with comma for filter parameter
    const filter = trackingCodes.length > 0 ? trackingCodes.join(",") : "";

    // Call our backend proxy CSV endpoint
    // In development: goes directly to localhost:5028
    // In production: uses relative path through nginx
    let url = `${BOXLEO_API_URL}/api/boxleo/orders/csv?page=1&per_page=5000&orders_type=leads&is_marketplace=all`;

    if (filter) {
      url += `&filter=${encodeURIComponent(filter)}`;
    }

    const response = await fetch(url, {
      method: "GET",
      headers: {
        accept: "text/csv",
      },
    });

    if (!response.ok) {
      throw new Error(`Boxleo request failed with status ${response.status}`);
    }

    const csvText = await response.text();
    exportCsv(csvText, "boxleo-orders.csv");
  } catch (error) {
    console.error(error);
    alert(
      "Failed to fetch Boxleo orders. Make sure the BoxleoProxy server is running."
    );
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
        exportCsv(response.data);
        break;
      case "eu":
        const responseEu = await getDataEu(trackingCodes);
        exportCsv(responseEu.data, "Chau au.csv");

        break;
      case "boxleo":
        await boxleoHandler(trackingCodes);
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
