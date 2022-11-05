import "./styles.css";
import { orderBy } from "lodash";

const textbox = document.getElementById("textbox");
const form = document.getElementById("form");
const btnSubmit = document.getElementById("btn-submit");
const spinner = document.getElementsByClassName("spinner-border")[0];
const url =
  "https://apiv2.shipadelivery.com/7151BFA4-D6DB-66EE-FFFF-2579E2541200/E53D8B22-9B05-48D1-8C1C-D126EF68296F/services/whl/v2/my-packages/";
const csvMIMEType = "data:text/csv;charset=utf-8,";

const jsonToCsv = (jsonData) => {
  const replacer = (key, value) => (value === null ? "" : value); // specify how you want to handle null values here
  const header = Object.keys(jsonData[0][0]);
  let csv = [
    header.join(","), // header row first
  ];
  for (const trackingHistories of jsonData) {
    csv.push(
      ...trackingHistories.map((row) =>
        header
          .map((fieldName) => JSON.stringify(row[fieldName], replacer))
          .join(",")
      )
    );
  }
  return csv.join("\r\n");
};

const exportCsv = (csv) => {
  var encodedUri = encodeURI(csvMIMEType + csv);
  var link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", "tracking.csv");
  document.body.appendChild(link); // Required for FF
  link.click();
};

const getData = async (trackingCodes) => {
  let result = [];

  for (const code of trackingCodes) {
    const response = await fetch(url + code, {
      method: "GET",
      headers: {
        "x-order-story-version": "v2",
      },
    });
    const data = await response.json();
    const orderHistory = orderBy(data.orderStory, ["date"], ["desc"]);
    result.push(
      orderHistory.map((h) => ({
        trackingCode: code,
        latestStatus: data.orderStatus,
        latestDate: new Date(orderHistory[0].date).toLocaleDateString(),
        orderDate: new Date(h.date).toLocaleDateString(),
        orderStatus: h.status,
      }))
    );
  }

  return result;
};

const clickHandler = async (e) => {
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
