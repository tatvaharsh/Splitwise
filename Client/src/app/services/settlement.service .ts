import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ILoginRequest, ILoginResponse } from "../models/auth.model";
import { Observable } from "rxjs";
import { IResponse } from "../generic/response";
import { GlobalConstant } from "../generic/global-const";
import { LocalStorageService } from "./storage.service";
import { FriendSettlementTransparency } from "../models/settlement-transparency";

@Injectable({
  providedIn: "root",
})
export class SettlementService {
    private apiUrl = `http://localhost:5158/api/Expense/`;
  
    http: HttpClient = inject(HttpClient);
    getFriendSettlementTransparency(friend2Id: string): Observable<IResponse<FriendSettlementTransparency>> {
        return this.http.get<IResponse<FriendSettlementTransparency>>(`${this.apiUrl}settle-summary/friends/${friend2Id}/transparency`);
      }
}