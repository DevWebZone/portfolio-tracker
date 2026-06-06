import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { API_BASE_URL } from '../api/client'
import type { Alert, MarketPrice, PortfolioSnapshot } from '../types'
import { alertGenerated, portfolioUpdated, priceUpdated, setConnectionStatus } from '../store/portfolioSlice'
import { useAppDispatch } from '../store/hooks'

export function usePortfolioHub() {
  const dispatch = useAppDispatch()

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/portfolio`)
      .withAutomaticReconnect()
      .build()

    connection.on('PriceUpdated', (event: MarketPrice) => dispatch(priceUpdated(event)))
    connection.on('PortfolioUpdated', (event: PortfolioSnapshot) => dispatch(portfolioUpdated(event)))
    connection.on('AlertGenerated', (event: Alert) => dispatch(alertGenerated(event)))

    connection.onreconnecting(() => dispatch(setConnectionStatus('connecting')))
    connection.onreconnected(() => dispatch(setConnectionStatus('connected')))
    connection.onclose(() => dispatch(setConnectionStatus('disconnected')))

    dispatch(setConnectionStatus('connecting'))
    connection
      .start()
      .then(() => dispatch(setConnectionStatus('connected')))
      .catch(() => dispatch(setConnectionStatus('disconnected')))

    return () => {
      void connection.stop()
    }
  }, [dispatch])
}
