export interface orderResponse {
  // orderStatus: string;
  barcode: string;
  orderStory: orderStory[];
}
export interface orderStory {
  date: Date;
  status: string;
  details?: {
    failReason: string;
    attemptNumber: number;
  };
}

export interface deliveryCheckResult {
  trackingCode: string;
  latestStatus: string;
  latestDate: string;
  orderDate: string;
  orderStatus: string;
  failReason: string;
}

// tracking info
// {
//   content: "Our delivery associate is out for delivery and will reach you shortly.";
//   trackStage: 1003;
//   trackStageTx: "Delivery";
//   time: "2023-05-27 07:01:26";
// },
export interface IMileRepsonse {
  resultObject: {
    waybillNo: string;
    trackInfos: IMileTrackingInfo[];
  };
}

export interface IMileTrackingInfo {
  content: string;
  trackStage: number;
  trackStageTx: string;
  time: string;
}
