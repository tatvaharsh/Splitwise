export interface IResponse<T> {
    statusCode: number;
    message: string;
    content: T;
    success: boolean;
  }