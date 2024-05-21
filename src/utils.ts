import { orderBy } from "lodash";
import {
  IMileRepsonse,
  IMileTrackingInfo,
  deliveryCheckResult,
  orderResponse,
  orderStory,
} from "./types";
import axios from "axios";
import rateLimit from "axios-rate-limit";
import { from, mergeMap, toArray, forkJoin } from "rxjs";
import { map, concatMap, catchError } from "rxjs/operators";

// Create a new instance of Axios with the rateLimit interceptor
const throttledAxios = rateLimit(axios.create(), {
  maxRequests: 300,
  perMilliseconds: 1000,
});

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

const urlImile = "/proxy?waybillNo=";
export const getDataIMile = async (trackingCodes: string[]) => {
  let result: deliveryCheckResult[] = [];

  let requestPromises: ReturnType<typeof axios.get<IMileRepsonse>>[] = [];

  for (const code of trackingCodes) {
    requestPromises.push(
      throttledAxios.get<IMileRepsonse>(urlImile + code, {
        headers: {
          "Content-Type": "application/json;charset=UTF-8",
          Accept: "application/json",
        },
      })
    );
  }

  const responses = await Promise.all(requestPromises);
  const data: IMileRepsonse[] = await Promise.all([
    ...responses.map((res) => res.data),
  ]);
  data.forEach((orderResponse) => {
    const sortedResponse: IMileTrackingInfo[] = orderBy(
      orderResponse.resultObject.trackInfos,
      ["time"],
      ["desc"]
    );

    result.push(
      ...sortedResponse.map((trackingInfo) => ({
        trackingCode: `${orderResponse.resultObject.waybillNo} `,
        latestStatus: sortedResponse[0].content,
        latestDate: new Date(trackingInfo.time).toLocaleDateString(),
        orderDate: `${new Date(
          trackingInfo.time
        ).toLocaleDateString()} ${new Date(
          trackingInfo.time
        ).toLocaleTimeString()}`,
        orderStatus: trackingInfo.content,
        failReason: "",
        failAttempt: "",
      }))
    );
  });

  return result;
};

export const getData = async (trackingCodes: string[]) => {
  let result: deliveryCheckResult[] = [];

  let requestPromises: ReturnType<typeof axios.get<orderResponse>>[] = [];

  for (const code of trackingCodes) {
    requestPromises.push(
      throttledAxios.get<orderResponse>(url + code, {
        headers: {
          "x-order-story-version": "v2",
        },
      })
    );
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
        estimateArrivalTime: new Date(
          orderResponse.dropOffEstimatedTime
        ).toLocaleString("en-US", {
          timeZone: "Asia/Ho_Chi_Minh",
        }),
      }))
    );
  });

  return result;
};

export const getDataRxJs = (trackingCodes: string[]) => {
  const requestObservables = trackingCodes.map((code) =>
    from(
      throttledAxios.get<orderResponse>(url + code, {
        headers: {
          "x-order-story-version": "v2",
        },
      })
    )
  );

  return forkJoin(requestObservables)
    .pipe(
      mergeMap((responses) =>
        from(responses).pipe(map((response) => response.data))
      ),
      mergeMap((orderResponse) =>
        from(orderResponse.orderStory).pipe(
          map((orderStory) => ({
            trackingCode: orderResponse.barcode,
            latestStatus: orderResponse.orderStory[0].status,
            latestDate: new Date(
              orderResponse.orderStory[0].date
            ).toLocaleDateString(),
            orderDate: new Date(orderStory.date).toLocaleDateString(),
            orderStatus: orderStory.status,
            failReason: orderStory.details?.failReason ?? "",
            failAttempt: orderStory.details?.attemptNumber ?? "",
          }))
        )
      ),
      toArray()
    )
    .toPromise();
};
