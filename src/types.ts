export interface orderResponse {
  // orderStatus: string;
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
