import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ILoginRequest, ILoginResponse } from "../models/auth.model";
import { Observable } from "rxjs";
import { IResponse } from "../generic/response";
import { GlobalConstant } from "../generic/global-const";
import { LocalStorageService } from "./storage.service";

@Injectable({
  providedIn: "root",
})
export class AuthService {
    private apiUrl = `http://localhost:5158/api/Auth/`;
  
    http: HttpClient = inject(HttpClient);
    store = inject(LocalStorageService);

    login(data: ILoginRequest): Observable<IResponse<ILoginResponse>> {
        return this.http.post<IResponse<ILoginResponse>>(
          `${this.apiUrl}login`, 
          data
        );
      }

    signup(formData: FormData): Observable<IResponse<null>> {
        return this.http.post<IResponse<null>>(`${this.apiUrl}register`, formData);
      }

    refreshAccessToken(
        refreshToken: string
      ): Observable<IResponse<ILoginResponse>> {
        return this.http.post<IResponse<ILoginResponse>>(`${this.apiUrl}refresh`, {
          refreshToken,
        });
    }

    check() {
        const token = this.store.get(GlobalConstant.ACCESS_TOKEN);
        return token != null;
    }
}