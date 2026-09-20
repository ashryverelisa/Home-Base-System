using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBase.Database.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW v_effective_line AS
                SELECT i.id,
                       i.purchase_id,
                       i.product_id,
                       i.quantity_base,
                       i.is_promo,
                       i.line_total + COALESCE(d.adj, 0) AS effective_total,
                       (i.line_total + COALESCE(d.adj, 0))
                           / NULLIF(i.quantity_base, 0) AS effective_price_per_base_unit
                FROM purchase_item i
                LEFT JOIN LATERAL (
                    SELECT SUM(c.line_total) AS adj
                    FROM purchase_item c
                    WHERE c.parent_item_id = i.id AND c.line_type = 'Discount'
                ) d ON true
                WHERE i.line_type = 'Item';
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_monthly_spend AS
                SELECT date_trunc('month', p.purchased_at) AS month,
                       p.store_id,
                       pr.category_id,
                       SUM(i.line_total) AS total
                FROM purchase_item i
                JOIN purchase p ON p.id = i.purchase_id
                LEFT JOIN product pr ON pr.id = i.product_id
                WHERE p.status = 'Confirmed'
                GROUP BY 1, 2, 3;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_price_history AS
                SELECT i.product_id,
                       p.store_id,
                       p.purchased_at::date AS day,
                       i.price_per_base_unit,
                       i.is_promo,
                       LAG(i.price_per_base_unit) OVER (
                           PARTITION BY i.product_id, p.store_id ORDER BY p.purchased_at
                       ) AS prev_price
                FROM purchase_item i
                JOIN purchase p ON p.id = i.purchase_id
                WHERE i.product_id IS NOT NULL
                  AND i.line_type = 'Item'
                  AND p.status = 'Confirmed';
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_deposit_balance AS
                SELECT SUM(i.line_total) AS open_deposit
                FROM purchase_item i
                JOIN purchase p ON p.id = i.purchase_id
                WHERE i.line_type IN ('Deposit', 'DepositReturn')
                  AND p.status = 'Confirmed';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_deposit_balance;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_price_history;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_monthly_spend;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_effective_line;");
        }
    }
}
