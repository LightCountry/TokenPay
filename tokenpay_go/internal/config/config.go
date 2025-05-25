package config

import (
	"encoding/json"
	"os"
	"tokenpay_go/internal/models"
)

// AppSettings represents the application configuration.
type AppSettings struct {
	Serilog             map[string]interface{} `json:"Serilog"`
	AllowedHosts        string                 `json:"AllowedHosts"`
	ConnectionStrings   ConnectionStrings      `json:"ConnectionStrings"`
	TronProApiKey       string                 `json:"TRON-PRO-API-KEY"`
	BaseCurrency        string                 `json:"BaseCurrency"`
	Rate                map[string]float64     `json:"Rate"` // Example: "USDT": 6.5
	ExpireTime          int                    `json:"ExpireTime"`
	UseDynamicAddress   bool                   `json:"UseDynamicAddress"`
	Address             map[string][]string    `json:"Address"` // Example: "TRON": ["address1", "address2"]
	OnlyConfirmed       bool                   `json:"OnlyConfirmed"`
	NotifyTimeOut       int                    `json:"NotifyTimeOut"`
	ApiToken            string                 `json:"ApiToken"`
	WebSiteUrl          string                 `json:"WebSiteUrl"`
	Collection          CollectionConfig       `json:"Collection"`
	Telegram            TelegramConfig         `json:"Telegram"`
	RateMove            map[string]float64     `json:"RateMove"` // Example: "TRX_CNY": 0.05
	DynamicAddressConfig DynamicAddressConfig   `json:"DynamicAddressConfig"`
}

// ConnectionStrings holds database connection strings.
type ConnectionStrings struct {
	DB string `json:"DB"`
}

// CollectionConfig holds settings for the collection service.
type CollectionConfig struct {
	Enable              bool    `json:"Enable"`
	UseEnergy           bool    `json:"UseEnergy"`
	ForceCheckAllAddress bool    `json:"ForceCheckAllAddress"`
	RetainUSDT          bool    `json:"RetainUSDT"`
	CheckTime           int     `json:"CheckTime"`
	MinUSDT             float64 `json:"MinUSDT"`
	NeedEnergy          int     `json:"NeedEnergy"`
	EnergyPrice         int     `json:"EnergyPrice"`
	Address             string  `json:"Address"`
}

// TelegramConfig holds Telegram bot settings.
type TelegramConfig struct {
	AdminUserId int64  `json:"AdminUserId"`
	BotToken    string `json:"BotToken"`
}

// DynamicAddressConfig holds settings for dynamic addresses.
type DynamicAddressConfig struct {
	AmountMove bool               `json:"AmountMove"`
	TRX        []float64          `json:"TRX"`
	USDT       []float64          `json:"USDT"`
	ETH        []float64          `json:"ETH"`
	// Add other coins as needed, e.g., BTC []float64 `json:"BTC"`
}

// EVMChainsConfigWrapper is a helper struct for unmarshalling EVM chains config.
type EVMChainsConfigWrapper struct {
	Chains []models.EVMChainConfig `json:"EVMChains"`
}

// LoadAppSettings reads and unmarshals the app settings from a JSON file.
func LoadAppSettings(filePath string) (*AppSettings, error) {
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, err
	}

	var settings AppSettings
	err = json.Unmarshal(data, &settings)
	if err != nil {
		return nil, err
	}
	return &settings, nil
}

// LoadEVMChainsConfig reads and unmarshals the EVM chains configuration from a JSON file.
func LoadEVMChainsConfig(filePath string) ([]models.EVMChainConfig, error) {
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, err
	}

	var wrapper EVMChainsConfigWrapper
	err = json.Unmarshal(data, &wrapper)
	if err != nil {
		return nil, err
	}
	return wrapper.Chains, nil
}
