import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  ButtonGroup,
  Chip,
  CircularProgress,
  Container,
  CssBaseline,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  ThemeProvider,
  Tooltip,
  Typography,
  createTheme,
} from '@mui/material'
import Grid from '@mui/material/Grid'
import { Activity, Bell, CircleDollarSign, RefreshCw, Send, TrendingUp, WalletCards } from 'lucide-react'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip as ChartTooltip,
  XAxis,
  YAxis,
} from 'recharts'
import './App.css'
import { loadDashboard, submitTrade } from './store/portfolioSlice'
import { useAppDispatch, useAppSelector } from './store/hooks'
import { usePortfolioHub } from './hooks/usePortfolioHub'

const theme = createTheme({
  palette: {
    mode: 'light',
    background: { default: '#f7f8fb', paper: '#ffffff' },
    primary: { main: '#2563eb' },
    secondary: { main: '#0f766e' },
    warning: { main: '#b45309' },
    error: { main: '#dc2626' },
    text: { primary: '#172033', secondary: '#5f6b7a' },
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: "Inter, ui-sans-serif, system-ui, 'Segoe UI', sans-serif",
    h1: { fontSize: 28, fontWeight: 700, letterSpacing: 0 },
    h2: { fontSize: 18, fontWeight: 700, letterSpacing: 0 },
    h3: { fontSize: 15, fontWeight: 700, letterSpacing: 0 },
    button: { textTransform: 'none', fontWeight: 700, letterSpacing: 0 },
  },
})

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const number = new Intl.NumberFormat('en-US', { maximumFractionDigits: 4 })
const chartColors = ['#2563eb', '#0f766e', '#b45309', '#7c3aed', '#be123c', '#047857']

function App() {
  const dispatch = useAppDispatch()
  const { snapshot, prices, alerts, status, connectionStatus, error } = useAppSelector((state) => state.portfolio)
  const [side, setSide] = useState<'buy' | 'sell'>('buy')
  const [symbol, setSymbol] = useState('AAPL')
  const [quantity, setQuantity] = useState('1')
  const [price, setPrice] = useState('190')

  usePortfolioHub()

  useEffect(() => {
    void dispatch(loadDashboard())
  }, [dispatch])

  useEffect(() => {
    const selected = prices.find((item) => item.symbol === symbol)
    if (selected && Number.isFinite(selected.currentPrice)) {
      setPrice(String(Number(selected.currentPrice.toFixed(2))))
    }
  }, [prices, symbol])

  const exposureData = useMemo(
    () => snapshot?.holdings.map((holding) => ({ name: holding.symbol, value: holding.exposurePercent })) ?? [],
    [snapshot],
  )

  const pnlData = useMemo(
    () =>
      snapshot?.holdings.map((holding) => ({
        symbol: holding.symbol,
        unrealized: holding.unrealizedPnL,
        marketValue: holding.marketValue,
      })) ?? [],
    [snapshot],
  )

  const submit = () => {
    const parsedQuantity = Number(quantity)
    const parsedPrice = Number(price)
    if (!symbol || parsedQuantity <= 0 || parsedPrice <= 0) return

    void dispatch(submitTrade({ side, trade: { symbol, quantity: parsedQuantity, price: parsedPrice } }))
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box className="app-shell">
        <Container maxWidth="xl" className="dashboard">
          <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, alignItems: { xs: 'stretch', md: 'center' }, justifyContent: 'space-between', gap: 2 }}>
            <Box>
              <Typography variant="h1">Portfolio Tracker</Typography>
              <Typography color="text.secondary">Live demo portfolio with simulated market ticks and intraday alerts.</Typography>
            </Box>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Chip
                icon={<Activity size={16} />}
                label={connectionStatus}
                color={connectionStatus === 'connected' ? 'success' : connectionStatus === 'connecting' ? 'warning' : 'default'}
                variant="outlined"
              />
              <Tooltip title="Refresh dashboard">
                <Button variant="outlined" startIcon={<RefreshCw size={17} />} onClick={() => void dispatch(loadDashboard())}>
                  Refresh
                </Button>
              </Tooltip>
            </Box>
          </Box>

          {error && <Alert severity="error">{error}</Alert>}
          {status === 'loading' && !snapshot ? (
            <Box className="loading-state">
              <CircularProgress />
            </Box>
          ) : (
            <>
              <Grid container spacing={2}>
                <Metric icon={<WalletCards />} label="Portfolio Value" value={money.format(snapshot?.totalValue ?? 0)} />
                <Metric icon={<TrendingUp />} label="Unrealized P&L" value={money.format(snapshot?.unrealizedPnL ?? 0)} tone={(snapshot?.unrealizedPnL ?? 0) >= 0 ? 'positive' : 'negative'} />
                <Metric icon={<CircleDollarSign />} label="Realized P&L" value={money.format(snapshot?.realizedPnL ?? 0)} />
                <Metric icon={<Bell />} label="P&L %" value={`${snapshot?.pnLPercent ?? 0}%`} tone={(snapshot?.pnLPercent ?? 0) >= 0 ? 'positive' : 'negative'} />
              </Grid>

              <Box className="market-ticker">
                {prices.map((item) => (
                  <Box key={item.symbol} className="ticker-item">
                    <strong>{item.symbol}</strong>
                    <span>{money.format(item.currentPrice)}</span>
                    <span className={item.movePercent >= 0 ? 'positive' : 'negative'}>{item.movePercent}%</span>
                  </Box>
                ))}
              </Box>

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, lg: 8 }}>
                  <Panel title="Holdings">
                    <Box className="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>Asset</th>
                            <th>Qty</th>
                            <th>Avg Buy</th>
                            <th>Price</th>
                            <th>Value</th>
                            <th>Unrealized</th>
                            <th>Exposure</th>
                          </tr>
                        </thead>
                        <tbody>
                          {snapshot?.holdings.map((holding) => (
                            <tr key={holding.symbol}>
                              <td>
                                <strong>{holding.symbol}</strong>
                                <span>{holding.name}</span>
                              </td>
                              <td>{number.format(holding.quantity)}</td>
                              <td>{money.format(holding.averageBuyPrice)}</td>
                              <td>{money.format(holding.currentPrice)}</td>
                              <td>{money.format(holding.marketValue)}</td>
                              <td className={holding.unrealizedPnL >= 0 ? 'positive' : 'negative'}>
                                {money.format(holding.unrealizedPnL)} ({holding.pnLPercent}%)
                              </td>
                              <td>{holding.exposurePercent}%</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </Box>
                  </Panel>
                </Grid>

                <Grid size={{ xs: 12, lg: 4 }}>
                  <Panel title="Trade Ticket">
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                      <ButtonGroup fullWidth>
                        <Button variant={side === 'buy' ? 'contained' : 'outlined'} onClick={() => setSide('buy')}>Buy</Button>
                        <Button variant={side === 'sell' ? 'contained' : 'outlined'} color="secondary" onClick={() => setSide('sell')}>Sell</Button>
                      </ButtonGroup>
                      <FormControl fullWidth size="small">
                        <InputLabel>Symbol</InputLabel>
                        <Select value={symbol} label="Symbol" onChange={(event) => setSymbol(event.target.value)}>
                          {prices.map((item) => (
                            <MenuItem key={item.symbol} value={item.symbol}>{item.symbol}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <TextField size="small" label="Quantity" type="number" value={quantity} onChange={(event) => setQuantity(event.target.value)} />
                      <TextField size="small" label="Price" type="number" value={price} onChange={(event) => setPrice(event.target.value)} />
                      <Button variant="contained" endIcon={<Send size={17} />} onClick={submit}>
                        Submit {side}
                      </Button>
                    </Box>
                  </Panel>
                </Grid>

                <Grid size={{ xs: 12, md: 6 }}>
                  <Panel title="Exposure">
                    <ResponsiveContainer width="100%" height={280}>
                      <PieChart>
                        <Pie data={exposureData} dataKey="value" nameKey="name" innerRadius={58} outerRadius={96} paddingAngle={2}>
                          {exposureData.map((entry, index) => (
                            <Cell key={entry.name} fill={chartColors[index % chartColors.length]} />
                          ))}
                        </Pie>
                        <ChartTooltip formatter={(value) => `${value}%`} />
                        <Legend />
                      </PieChart>
                    </ResponsiveContainer>
                  </Panel>
                </Grid>

                <Grid size={{ xs: 12, md: 6 }}>
                  <Panel title="P&L by Asset">
                    <ResponsiveContainer width="100%" height={280}>
                      <BarChart data={pnlData}>
                        <CartesianGrid strokeDasharray="3 3" vertical={false} />
                        <XAxis dataKey="symbol" />
                        <YAxis />
                        <ChartTooltip formatter={(value) => money.format(Number(value))} />
                        <Bar dataKey="unrealized" fill="#0f766e" radius={[4, 4, 0, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </Panel>
                </Grid>

                <Grid size={{ xs: 12, md: 7 }}>
                  <Panel title="Market Value">
                    <ResponsiveContainer width="100%" height={240}>
                      <AreaChart data={pnlData}>
                        <CartesianGrid strokeDasharray="3 3" vertical={false} />
                        <XAxis dataKey="symbol" />
                        <YAxis />
                        <ChartTooltip formatter={(value) => money.format(Number(value))} />
                        <Area type="monotone" dataKey="marketValue" stroke="#2563eb" fill="#bfdbfe" />
                      </AreaChart>
                    </ResponsiveContainer>
                  </Panel>
                </Grid>

                <Grid size={{ xs: 12, md: 5 }}>
                  <Panel title="Alerts">
                    <Box className="alert-list">
                      {alerts.length === 0 ? (
                        <Typography color="text.secondary">No price alerts have triggered yet.</Typography>
                      ) : (
                        alerts.slice(0, 8).map((alert) => (
                          <Box key={alert.id} className={`alert-row ${severityLabel(alert.severity).toLowerCase()}`}>
                            <div>
                              <strong>{alert.symbol}</strong>
                              <span>{alert.direction}{alert.threshold}% threshold</span>
                            </div>
                            <Chip size="small" label={severityLabel(alert.severity)} color={severityLabel(alert.severity) === 'Critical' ? 'error' : 'warning'} />
                          </Box>
                        ))
                      )}
                    </Box>
                  </Panel>
                </Grid>
              </Grid>
            </>
          )}
        </Container>
      </Box>
    </ThemeProvider>
  )
}

function Metric({ icon, label, value, tone }: { icon: React.ReactNode; label: string; value: string; tone?: 'positive' | 'negative' }) {
  return (
    <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
      <Box className="metric">
        <span className="metric-icon">{icon}</span>
        <div>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
          <Typography variant="h2" className={tone}>{value}</Typography>
        </div>
      </Box>
    </Grid>
  )
}

function Panel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Box className="panel">
      <Typography variant="h3">{title}</Typography>
      {children}
    </Box>
  )
}

function severityLabel(severity: unknown) {
  if (severity === 'Critical' || severity === 1) return 'Critical'
  return 'Warning'
}

export default App
