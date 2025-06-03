import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ILoginRequest, ILoginResponse } from "../models/auth.model";
import { Observable } from "rxjs";
import { IResponse } from "../generic/response";

@Injectable({
  providedIn: "root",
})
export class AuthService {
    private apiUrl = `http://localhost:5158/api/Auth/`;
  
    http: HttpClient = inject(HttpClient);

    login(data: ILoginRequest): Observable<IResponse<ILoginResponse>> {
        return this.http.post<IResponse<ILoginResponse>>(
          `${this.apiUrl}login`, 
          data
        );
      }

    signup(formData: FormData): Observable<IResponse<null>> {
        return this.http.post<IResponse<null>>(`${this.apiUrl}register`, formData);
      }
}