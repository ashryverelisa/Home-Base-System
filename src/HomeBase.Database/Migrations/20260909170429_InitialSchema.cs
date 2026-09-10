using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeBase.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_category_parent_id",
                        column: x => x.parent_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    servings = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    prep_minutes = table.Column<int>(type: "integer", nullable: true),
                    cook_minutes = table.Column<int>(type: "integer", nullable: true),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "storage_location",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    zone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_location", x => x.id);
                    table.ForeignKey(
                        name: "fk_storage_location_storage_location_parent_id",
                        column: x => x.parent_id,
                        principalTable: "storage_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "store",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    chain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    tax_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meal_plan_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_date = table.Column<DateOnly>(type: "date", nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipe_id = table.Column<int>(type: "integer", nullable: true),
                    free_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    servings = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cooked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_plan_entry", x => x.id);
                    table.CheckConstraint("ck_meal_plan_entry_recipe_or_text", "recipe_id IS NOT NULL OR free_text IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_meal_plan_entry_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    base_unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    package_size = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false, defaultValue: 1m),
                    piece_weight_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    is_food = table.Column<bool>(type: "boolean", nullable: false),
                    min_stock_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    target_stock_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    default_shelf_life_days = table.Column<int>(type: "integer", nullable: true),
                    default_location_id = table.Column<int>(type: "integer", nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    off_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_storage_locations_default_location_id",
                        column: x => x.default_location_id,
                        principalTable: "storage_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchase",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<int>(type: "integer", nullable: true),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "EUR"),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    receipt_file = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_alias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    store_id = table.Column<int>(type: "integer", nullable: true),
                    raw_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    times_seen = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_alias_products_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_alias_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_ingredient",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipe_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    free_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_ingredient", x => x.id);
                    table.CheckConstraint("ck_recipe_ingredient_product_or_text", "product_id IS NOT NULL OR free_text IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_recipe_ingredient_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_ingredient_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_id = table.Column<long>(type: "bigint", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    line_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    parent_item_id = table.Column<long>(type: "bigint", nullable: true),
                    raw_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(14,6)", precision: 14, scale: 6, nullable: true),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_promo = table.Column<bool>(type: "boolean", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    price_per_base_unit = table.Column<decimal>(type: "numeric(14,6)", precision: 14, scale: 6, nullable: true, computedColumnSql: "line_total / NULLIF(quantity_base, 0)", stored: true),
                    match_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    match_confidence = table.Column<float>(type: "real", nullable: true),
                    suggested_product_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_item_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_item_product_suggested_product_id",
                        column: x => x.suggested_product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_purchase_item_purchase_item_parent_item_id",
                        column: x => x.parent_item_id,
                        principalTable: "purchase_item",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_item_purchase_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchase",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    purchase_item_id = table.Column<long>(type: "bigint", nullable: true),
                    purchased_at = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    warranty_until = table.Column<DateOnly>(type: "date", nullable: true),
                    current_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    disposed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asset_products_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_asset_purchase_items_purchase_item_id",
                        column: x => x.purchase_item_id,
                        principalTable: "purchase_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_asset_storage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "storage_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    list_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    free_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    target_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    added_by = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    bought_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purchase_item_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_list_item", x => x.id);
                    table.CheckConstraint("ck_shopping_list_item_product_or_text", "product_id IS NOT NULL OR free_text IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_shopping_list_item_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_purchase_item_purchase_item_id",
                        column: x => x.purchase_item_id,
                        principalTable: "purchase_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shopping_list_item_shopping_list_list_id",
                        column: x => x.list_id,
                        principalTable: "shopping_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_lot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<int>(type: "integer", nullable: true),
                    quantity_base = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    best_before = table.Column<DateOnly>(type: "date", nullable: true),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purchase_item_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_lot", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_lot_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_lot_purchase_item_purchase_item_id",
                        column: x => x.purchase_item_id,
                        principalTable: "purchase_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_stock_lot_storage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "storage_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "asset_document",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_document", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_document_asset_asset_id",
                        column: x => x.asset_id,
                        principalTable: "asset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lot_id = table.Column<long>(type: "bigint", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meal_plan_entry_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movement_meal_plan_entry_meal_plan_entry_id",
                        column: x => x.meal_plan_entry_id,
                        principalTable: "meal_plan_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_stock_movement_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movement_stock_lot_lot_id",
                        column: x => x.lot_id,
                        principalTable: "stock_lot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_category_id",
                table: "asset",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_location_id",
                table: "asset",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_product_id",
                table: "asset",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_purchase_item_id",
                table: "asset",
                column: "purchase_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_serial_number",
                table: "asset",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_asset_document_asset_id",
                table: "asset_document",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_parent_id_name",
                table: "category",
                columns: new[] { "parent_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_entry_plan_date",
                table: "meal_plan_entry",
                column: "plan_date");

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_entry_recipe_id",
                table: "meal_plan_entry",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_category_id",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_default_location_id",
                table: "product",
                column: "default_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_gtin",
                table: "product",
                column: "gtin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_name",
                table: "product",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_alias_normalized_text",
                table: "product_alias",
                column: "normalized_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_alias_product_id",
                table: "product_alias",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_alias_store_id_normalized_text",
                table: "product_alias",
                columns: new[] { "store_id", "normalized_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_external_id",
                table: "purchase",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_purchased_at",
                table: "purchase",
                column: "purchased_at");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_store_id",
                table: "purchase",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_parent_item_id",
                table: "purchase_item",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_product_id",
                table: "purchase_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_purchase_id_line_no",
                table: "purchase_item",
                columns: new[] { "purchase_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_suggested_product_id",
                table: "purchase_item",
                column: "suggested_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_name",
                table: "recipe",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ingredient_product_id",
                table: "recipe_ingredient",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ingredient_recipe_id",
                table: "recipe_ingredient",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_list_id_status_added_by",
                table: "shopping_list_item",
                columns: new[] { "list_id", "status", "added_by" });

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_product_id",
                table: "shopping_list_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_purchase_item_id",
                table: "shopping_list_item",
                column: "purchase_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_lot_best_before",
                table: "stock_lot",
                column: "best_before",
                filter: "quantity_base > 0");

            migrationBuilder.CreateIndex(
                name: "ix_stock_lot_location_id",
                table: "stock_lot",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_lot_product_id",
                table: "stock_lot",
                column: "product_id",
                filter: "quantity_base > 0");

            migrationBuilder.CreateIndex(
                name: "ix_stock_lot_purchase_item_id",
                table: "stock_lot",
                column: "purchase_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_lot_id",
                table: "stock_movement",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_meal_plan_entry_id",
                table: "stock_movement",
                column: "meal_plan_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_product_id_occurred_at",
                table: "stock_movement",
                columns: new[] { "product_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_storage_location_parent_id",
                table: "storage_location",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_document");

            migrationBuilder.DropTable(
                name: "product_alias");

            migrationBuilder.DropTable(
                name: "recipe_ingredient");

            migrationBuilder.DropTable(
                name: "shopping_list_item");

            migrationBuilder.DropTable(
                name: "stock_movement");

            migrationBuilder.DropTable(
                name: "asset");

            migrationBuilder.DropTable(
                name: "shopping_list");

            migrationBuilder.DropTable(
                name: "meal_plan_entry");

            migrationBuilder.DropTable(
                name: "stock_lot");

            migrationBuilder.DropTable(
                name: "recipe");

            migrationBuilder.DropTable(
                name: "purchase_item");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "purchase");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "storage_location");

            migrationBuilder.DropTable(
                name: "store");
        }
    }
}
