import { orderBy } from "lodash";
import { deliveryCheckResult, orderResponse, orderStory } from "./types";
import axios from "axios";
import rateLimit from "axios-rate-limit";

// Create a new instance of Axios with the rateLimit interceptor
const throttledAxios = rateLimit(axios.create(), {
  maxRequests: 300,
  perMilliseconds: 1000,
});

export function splitArrayToChunks(arr: any[], chunkSize: number): any[] {
  const chunks = [];
  for (let i = 0; i < arr.length; i += chunkSize) {
    chunks.push(arr.slice(i, i + chunkSize));
  }
  return chunks;
}

export function jsonToCsv(deliveryCheckResults: deliveryCheckResult[]) {
  const replacer = (_key: any, value: any) => (value === null ? "" : value); // specify how you want to handle null values here
  const header = Object.keys(deliveryCheckResults[0]);
  const csv = [header.join(",")];

  for (const result of deliveryCheckResults) {
    csv.push(
      header
        .map((fieldName) =>
          JSON.stringify(
            result[fieldName as keyof deliveryCheckResult],
            replacer
          )
        )
        .join(",")
    );
  }
  return csv.join("\r\n");
}

export function exportCsv(csv: string) {
  const csvMIMEType = "data:text/csv;charset=utf-8,";
  var encodedUri = encodeURI(csvMIMEType + csv);
  var link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", "tracking.csv");
  document.body.appendChild(link); // Required for FF
  link.click();
}

const url =
  "https://apiv2.shipadelivery.com/7151BFA4-D6DB-66EE-FFFF-2579E2541200/E53D8B22-9B05-48D1-8C1C-D126EF68296F/services/whl/v2/my-packages/";

export const getData = async (trackingCodes: string[]) => {
  let result: deliveryCheckResult[] = [];

  const arrayOfArrays = splitArrayToChunks(trackingCodes, 100);
  let requestPromises: ReturnType<typeof axios.get<orderResponse>>[] = [];

  for (const arr of arrayOfArrays) {
    for (const code of arr) {
      requestPromises.push(
        throttledAxios.get<orderResponse>(url + code, {
          headers: {
            "x-order-story-version": "v2",
          },
        })
      );
    }
  }

  const responses = await Promise.all(requestPromises);
  const data: orderResponse[] = await Promise.all([
    ...responses.map((res) => res.data),
  ]);
  data.forEach((orderResponse) => {
    const orderHistory: orderStory[] = orderBy(
      orderResponse.orderStory,
      ["date"],
      ["desc"]
    );

    result.push(
      ...orderHistory.map((h) => ({
        trackingCode: orderResponse.barcode,
        latestStatus: orderHistory[0].status,
        latestDate: new Date(orderHistory[0].date).toLocaleDateString(),
        orderDate: new Date(h.date).toLocaleDateString(),
        orderStatus: h.status,
        failReason: h.details?.failReason ?? "",
        failAttempt: h.details?.attemptNumber ?? "",
      }))
    );
  });

  return result;
};
