using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBase.Database.Migrations
{
    /// <inheritdoc />
    public partial class InsightViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW v_best_store AS
                SELECT e.product_id,
                       p.store_id,
                       AVG(e.effective_price_per_base_unit) AS average_price,
                       MIN(e.effective_price_per_base_unit) AS best_price,
                       COUNT(*)                             AS purchases,
                       MAX(p.purchased_at)                  AS last_seen
                FROM v_effective_line e
                JOIN purchase p ON p.id = e.purchase_id
                WHERE e.product_id IS NOT NULL
                  AND e.effective_price_per_base_unit IS NOT NULL
                  AND NOT e.is_promo
                  AND p.status = 'Confirmed'
                GROUP BY e.product_id, p.store_id;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_consumption_rate AS
                SELECT m.product_id,
                       SUM(-m.quantity_delta) / 90.0 * 7 AS per_week
                FROM stock_movement m
                WHERE m.type = 'Consume'
                  AND m.occurred_at >= now() - interval '90 days'
                GROUP BY m.product_id
                HAVING SUM(-m.quantity_delta) > 0;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_reach_days AS
                SELECT s.product_id,
                       s.stock_base,
                       c.per_week,
                       s.stock_base / NULLIF(c.per_week / 7.0, 0) AS days_left
                FROM (
                    SELECT product_id, SUM(quantity_base) AS stock_base
                    FROM stock_lot
                    WHERE quantity_base > 0
                    GROUP BY product_id
                ) s
                JOIN v_consumption_rate c ON c.product_id = s.product_id;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_waste_cost AS
                SELECT date_trunc('month', m.occurred_at) AS month,
                       m.product_id,
                       pr.category_id,
                       SUM(-m.quantity_delta)             AS quantity_base,
                       SUM(-m.quantity_delta * COALESCE(i.price_per_base_unit, 0)) AS cost
                FROM stock_movement m
                JOIN product pr ON pr.id = m.product_id
                LEFT JOIN stock_lot l ON l.id = m.lot_id
                LEFT JOIN purchase_item i ON i.id = l.purchase_item_id
                WHERE m.type = 'Waste'
                GROUP BY 1, m.product_id, pr.category_id;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_basket_index AS
                WITH monthly AS (
                    SELECT i.product_id,
                           date_trunc('month', p.purchased_at) AS month,
                           SUM(i.line_total) / NULLIF(SUM(i.quantity_base), 0) AS price
                    FROM purchase_item i
                    JOIN purchase p ON p.id = i.purchase_id
                    WHERE i.line_type = 'Item'
                      AND i.product_id IS NOT NULL
                      AND i.quantity_base > 0
                      AND NOT i.is_promo
                      AND p.status = 'Confirmed'
                    GROUP BY i.product_id, 2
                ),
                base AS (
                    SELECT DISTINCT ON (product_id) product_id, price
                    FROM monthly
                    WHERE price > 0
                    ORDER BY product_id, month
                ),
                weight AS (
                    SELECT i.product_id, SUM(i.quantity_base) AS quantity_base
                    FROM purchase_item i
                    JOIN purchase p ON p.id = i.purchase_id
                    WHERE i.line_type = 'Item'
                      AND i.product_id IS NOT NULL
                      AND p.status = 'Confirmed'
                    GROUP BY i.product_id
                )
                SELECT m.month,
                       SUM(m.price / b.price * w.quantity_base)
                           / NULLIF(SUM(w.quantity_base), 0) * 100 AS index_value,
                       COUNT(*)                                    AS products
                FROM monthly m
                JOIN base b ON b.product_id = m.product_id
                JOIN weight w ON w.product_id = m.product_id
                GROUP BY m.month;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE VIEW v_promo_savings AS
                SELECT date_trunc('month', p.purchased_at) AS month,
                       p.store_id,
                       SUM(i.discount) AS discount_total,
                       SUM(GREATEST(ref.normal_price - i.price_per_base_unit, 0)
                           * COALESCE(i.quantity_base, 0)) AS saved_against_normal
                FROM purchase_item i
                JOIN purchase p ON p.id = i.purchase_id
                LEFT JOIN LATERAL (
                    SELECT percentile_cont(0.5) WITHIN GROUP (ORDER BY x.price_per_base_unit)
                           AS normal_price
                    FROM purchase_item x
                    JOIN purchase xp ON xp.id = x.purchase_id
                    WHERE x.product_id = i.product_id
                      AND x.line_type = 'Item'
                      AND NOT x.is_promo
                      AND xp.purchased_at < p.purchased_at
                      AND xp.status = 'Confirmed'
                ) ref ON true
                WHERE i.is_promo
                  AND i.line_type = 'Item'
                  AND p.status = 'Confirmed'
                GROUP BY 1, p.store_id;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_promo_savings;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_basket_index;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_waste_cost;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_reach_days;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_consumption_rate;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_best_store;");
        }
    }
}
