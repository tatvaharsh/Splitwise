import { MatSnackBar, MatSnackBarHorizontalPosition, MatSnackBarVerticalPosition } from "@angular/material/snack-bar";

export class SnackbarConfig {
    message: string;
    action?: string | undefined;
    status?: "success" | "error" | "warning" | "info";
    duration?: number;
    horizontalPosition?: MatSnackBarHorizontalPosition;
    verticalPosition?: MatSnackBarVerticalPosition;
    panelClass?: string | string[];
    icon?: "done" | "report-problem" | "warning_amber" | "info";
    _snackbar?: MatSnackBar

    constructor(options?: Partial<SnackbarConfig>) {
        this.message = "";
        this.duration = 3000;
        this.horizontalPosition = "right";
        this.verticalPosition = "top";
        this.panelClass = "";
        this.status = "success";
        this.action = "Close";
        this._snackbar = undefined;
        this.icon = "done";

        if (options)
            Object.assign(this, options);
    }
}
