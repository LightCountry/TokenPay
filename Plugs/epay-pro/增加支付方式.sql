-- TokenPay 支付方式
INSERT INTO `pay_type` (`name`, `device`, `showname`, `status`)
SELECT source.`name`, 0, source.`showname`, 1
FROM (
    SELECT 'TRX' AS `name`, 'TRX (TRON)' AS `showname`
    UNION ALL SELECT 'USDT_TRC20', 'USDT (TRON)'
    UNION ALL SELECT 'EVM_ETH_ETH', 'ETH (Ethereum)'
    UNION ALL SELECT 'EVM_ETH_USDT_ERC20', 'USDT (Ethereum)'
    UNION ALL SELECT 'EVM_ETH_USDC_ERC20', 'USDC (Ethereum)'
    UNION ALL SELECT 'EVM_BSC_BNB', 'BNB (BSC)'
    UNION ALL SELECT 'EVM_BSC_USDT_BEP20', 'USDT (BSC)'
    UNION ALL SELECT 'EVM_BSC_USDC_BEP20', 'USDC (BSC)'
    UNION ALL SELECT 'EVM_Polygon_POL', 'POL (Polygon)'
    UNION ALL SELECT 'EVM_Polygon_USDT_ERC20', 'USDT (Polygon)'
    UNION ALL SELECT 'EVM_Polygon_USDC_ERC20', 'USDC (Polygon)'
) AS source
LEFT JOIN `pay_type` AS target ON target.`name` = source.`name` AND target.`device` = 0
WHERE target.`id` IS NULL;
