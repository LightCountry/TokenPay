package models

import "time"

// TokenOrder represents an order in the system.
type TokenOrder struct {
	ID                 string     `json:"id"`
	OutOrderID         string     `json:"outOrderId"`
	OrderUserKey       string     `json:"orderUserKey"`
	BlockTransactionID *string    `json:"blockTransactionId"`
	PayTime            *time.Time `json:"payTime"`
	PayAmount          *float64   `json:"payAmount"`
	IsDynamicAmount    bool       `json:"isDynamicAmount"`
	FromAddress        *string    `json:"fromAddress"`
	ActualAmount       float64    `json:"actualAmount"`
	Currency           string     `json:"currency"`
	Amount             float64    `json:"amount"`
	ToAddress          string     `json:"toAddress"`
	Status             string     `json:"status"` // Pending, Paid, Expired
	PassThroughInfo    *string    `json:"passThroughInfo"`
	NotifyURL          *string    `json:"notifyUrl"`
	RedirectURL        *string    `json:"redirectUrl"`
	CallbackNum        int        `json:"callbackNum"`
	CallbackConfirm    bool       `json:"callbackConfirm"`
	LastNotifyTime     *time.Time `json:"lastNotifyTime"`
	CreateTime         time.Time  `json:"createTime"`
}

// OrderStatus constants
const (
	OrderStatusPending = "Pending"
	OrderStatusPaid    = "Paid"
	OrderStatusExpired = "Expired"
)

// TokenRate represents the exchange rate for a token.
type TokenRate struct {
	ID             string    `json:"id"`
	Currency       string    `json:"currency"`
	FiatCurrency   string    `json:"fiatCurrency"` // CNY, USD
	Rate           float64   `json:"rate"`
	LastUpdateTime time.Time `json:"lastUpdateTime"`
}

// FiatCurrency constants
const (
	FiatCurrencyCNY = "CNY"
	FiatCurrencyUSD = "USD"
	FiatCurrencyEUR = "EUR"
)

// TokenWallet represents a wallet for a token.
type TokenWallet struct {
	ID            string     `json:"id"`
	Address       string     `json:"address"`
	Key           string     `json:"key"` // Consider secure storage
	ChainType     string     `json:"chainType"` // EVM, TRX
	Balance       float64    `json:"balance"`
	USDTBalance   float64    `json:"usdtBalance"`
	LastCheckTime *time.Time `json:"lastCheckTime"`
}

// TokenCurrencyType constants
const (
	TokenCurrencyTypeEVM = "EVM"
	TokenCurrencyTypeTRX = "TRX"
)

// CreateOrderViewModel represents the data needed to create an order.
type CreateOrderViewModel struct {
	OutOrderID      string   `json:"outOrderId"`
	OrderUserKey    string   `json:"orderUserKey"`
	ActualAmount    float64  `json:"actualAmount"`
	Currency        string   `json:"currency"`
	PassThroughInfo *string  `json:"passThroughInfo"`
	NotifyURL       *string  `json:"notifyUrl"`
	RedirectURL     *string  `json:"redirectUrl"`
	Signature       *string  `json:"signature"`
}

// ReturnData represents a generic API response.
type ReturnData struct {
	Success bool                   `json:"success"`
	Message *string                `json:"message"`
	Data    interface{}            `json:"data"`
	Info    map[string]interface{} `json:"info"`
}

// TransactionNotification represents a transaction notification.
type TransactionNotification struct {
	Address       string   `json:"address"`
	TxID          string   `json:"txId"`
	Time          int64    `json:"time"` // Unix timestamp
	Confirmations int      `json:"confirmations"`
	Value         float64  `json:"value"` // Amount of native coin
	Chain         string   `json:"chain"` // ETH, TRX
	Height        int64    `json:"height"` // Block height
	TokenAddress  *string  `json:"tokenAddress"`
	TokenSymbol   *string  `json:"tokenSymbol"`
	TokenValue    *float64 `json:"tokenValue"`
	TokenReceivingAddress string `json:"tokenReceivingAddress,omitempty"`
}

// ChainType constants
const (
	ChainTypeETH = "ETH"
	ChainTypeTRX = "TRX"
)

// EVMChainConfig represents the configuration for an EVM chain.
type EVMChainConfig struct {
	Enable        bool             `json:"enable"`
	ChainName     string           `json:"chainName"`
	ChainNameEN   string           `json:"chainNameEn"`
	BaseCoin      string           `json:"baseCoin"`
	Confirmations int              `json:"confirmations"`
	Decimals      int              `json:"decimals"`
	ScanHost      string           `json:"scanHost"`
	ApiHost       string           `json:"apiHost"`
	ApiKey        string           `json:"apiKey"`
	ERC20Name     string           `json:"erc20Name"`
	ERC20Tokens   []EVMErc20Token `json:"erc20Tokens"`
}

// EVMErc20Token represents an ERC20 token.
type EVMErc20Token struct {
	Name            string `json:"name"`
	ContractAddress string `json:"contractAddress"`
}
