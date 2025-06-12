import { Component, OnInit } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { MemberAggregationDetailDto, SettleSummaryExplanationResponseDto } from '../../models/settlement-transparency';
import { ActivatedRoute } from '@angular/router';
import { SettlementService } from '../../services/settlement.service ';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { IResponse } from '../../generic/response';

@Component({
  selector: 'app-settlement-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settlement-summary.component.html',
  styleUrl: './settlement-summary.component.css'
})
export class SettlementSummaryComponent implements OnInit {
  groupId: string = '';
  settlementSummary$!: SettleSummaryExplanationResponseDto;

  constructor(
    private route: ActivatedRoute,
    private settlementService: SettlementService,
    private sanitizer: DomSanitizer // Inject DomSanitizer
  ) { }

  ngOnInit(): void {   

    this.route.paramMap.subscribe(summary => {
      this.groupId = summary.get('id') ?? "";
      this.settlementService.getSettleSummaryExplained(this.groupId).subscribe(summary => {
        this.settlementSummary$ = summary.content;
      });
    });
  }

  public sanitizeHtml(markdown: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(this.convertMarkdownToHtml(markdown));
  }
  // --- Helper function to generate the initial explanation with transparency ---
  generateInitialStateExplanation(
    initialDetailedDebts: { [key: string]: number },
    memberAggregationDetails: MemberAggregationDetailDto[],
    initialNetBalances: { [key: string]: number },
    memberNames: { [key: string]: string }
  ): SafeHtml {
    let explanationText = `### Initial State: Balances from Group Activities\n\n`;
    explanationText += `This section details the direct financial obligations arising from group expenses and how they aggregate into each member's overall net balance.\n`;

    // Detailed Debts (from responseData.initialDetailedDebtsFromActivities)
    if (Object.keys(initialDetailedDebts).length > 0) {
        explanationText += `\n**Detailed Debts (Who Owes Whom based on specific activity shares):**\n`;
        for (const debtKey in initialDetailedDebts) {
            explanationText += `- ${debtKey} $${initialDetailedDebts[debtKey].toFixed(2)}\n`;
        }
    } else {
        explanationText += `\n*No specific direct debts from group activities were recorded, or all initial debts self-cancelled.*\n`;
    }

    // Bridging Direct Debts to Net Balances (from responseData.memberAggregationDetails)
    if (memberAggregationDetails && memberAggregationDetails.length > 0) {
        explanationText += `\n**Bridging Direct Debts to Net Balances (Internal Aggregation Logic):**\n`;
        explanationText += `The system aggregates all individual 'who owes whom' debts to determine each person's single net financial position within the group.\n`;

        memberAggregationDetails.forEach(detail => {
            explanationText += `- **${detail.memberName}:**\n`;
            explanationText += `  * Total owed to them (from detailed debts): $${detail.totalOwedToThem.toFixed(2)}\n`;
            explanationText += `  * Total they owe others (from detailed debts): $${detail.totalTheyOweOthers.toFixed(2)}\n`;
            explanationText += `  * **Calculated Net Balance (Owed To - Owed By): $${detail.totalOwedToThem.toFixed(2)} - $${detail.totalTheyOweOthers.toFixed(2)} = $${detail.calculatedNetBalance.toFixed(2)}**\n`;
            if (detail.balancesMatch) {
                explanationText += `  * **This calculated net balance perfectly matches the overall net balance determined by the system ($${detail.actualServiceNetBalance.toFixed(2)}).**\n`;
            } else {
                explanationText += `  * **Note: The calculated net balance ($${detail.calculatedNetBalance.toFixed(2)}) varies slightly from the system's overall net balance ($${detail.actualServiceNetBalance.toFixed(2)}). This might be due to rounding or other internal calculations.**\n`;
            }
        });
    }

    // Overall Net Balances (from responseData.initialNetBalancesFromActivities)
    explanationText += `\n**Overall Net Balances after Group Activities (as determined by the system):**\n`;
    for (const memberId in initialNetBalances) {
        const balance = initialNetBalances[memberId];
        const memberName = memberNames[memberId];
        const type = balance > 0 ? "Creditor" : (balance < 0 ? "Debtor" : "Settled");
        explanationText += `- ${memberName}: $${balance.toFixed(2)} (${type})\n`;
    }

    // Sanitize the HTML for safe rendering in the template
    return this.sanitizer.bypassSecurityTrustHtml(this.convertMarkdownToHtml(explanationText));
  }

  // A very basic markdown to HTML converter (for illustration).
  // For production, consider a dedicated Angular markdown library like 'ngx-markdown'.
  private convertMarkdownToHtml(markdown: string): string {
    let html = markdown;
    html = html.replace(/### (.*)/g, '<h3>$1</h3>'); // Headings
    html = html.replace(/## (.*)/g, '<h2>$1</h2>');
    html = html.replace(/\n\n/g, '<p>'); // Paragraphs (basic)
    html = html.replace(/\n/g, '<br>'); // Line breaks
    html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>'); // Bold
    html = html.replace(/\*(.*?)\*/g, '<em>$1</em>'); // Italic
    html = html.replace(/^- (.*)/g, '<li>$1</li>'); // Lists (simple)
    html = html.replace(/\n<li>/g, '<ul><li>'); // Start of list
    html = html.replace(/<\/li>\n(?!<li>)/g, '</li></ul>'); // End of list
    // Add more conversions for other markdown elements as needed
    return html;
  }
}