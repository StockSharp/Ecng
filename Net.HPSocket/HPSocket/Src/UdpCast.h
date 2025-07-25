﻿/*
 * Copyright: JessMA Open Source (ldcsaa@gmail.com)
 *
 * Author	: Bruce Liang
 * Website	: https://github.com/ldcsaa
 * Project	: https://github.com/ldcsaa/HP-Socket
 * Blog		: http://www.cnblogs.com/ldcsaa
 * Wiki		: http://www.oschina.net/p/hp-socket
 * QQ Group	: 44636872, 75375912
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#pragma once

#include "SocketHelper.h"

#ifdef _UDP_SUPPORT

class CUdpCast : public IUdpCast
{
public:
	virtual BOOL Start	(LPCTSTR lpszRemoteAddress, USHORT usPort, BOOL bAsyncConnect = TRUE, LPCTSTR lpszBindAddress = nullptr, USHORT usLocalPort = 0);
	virtual BOOL Stop	();
	virtual BOOL Send	(const BYTE* pBuffer, int iLength, int iOffset = 0);
	virtual BOOL SendPackets	(const WSABUF pBuffers[], int iCount);
	virtual BOOL PauseReceive	(BOOL bPause = TRUE);
	virtual BOOL Wait			(DWORD dwMilliseconds = INFINITE) {return m_evWait.Wait(dwMilliseconds);}
	virtual BOOL			HasStarted			()	{return m_enState == SS_STARTED || m_enState == SS_STARTING;}
	virtual EnServiceState	GetState			()	{return m_enState;}
	virtual CONNID			GetConnectionID		()	{return m_dwConnID;}
	virtual EnSocketError	GetLastError		()	{return m_enLastError;}
	virtual LPCTSTR			GetLastErrorDesc	()	{return ::GetSocketErrorDesc(m_enLastError);}

	virtual BOOL GetLocalAddress		(TCHAR lpszAddress[], int& iAddressLen, USHORT& usPort);
	virtual BOOL GetRemoteHost			(TCHAR lpszHost[], int& iHostLen, USHORT& usPort);
	virtual BOOL GetPendingDataLength	(int& iPending) {iPending = m_iPending; return HasStarted();}
	virtual BOOL IsPauseReceive			(BOOL& bPaused) {bPaused = m_bPaused; return HasStarted();}
	virtual BOOL IsConnected			()				{return m_bConnected;}

public:
	virtual BOOL IsSecure				() {return FALSE;}

	virtual void SetReuseAddressPolicy	(EnReuseAddressPolicy enReusePolicy){ENSURE_HAS_STOPPED(); m_enReusePolicy			= enReusePolicy;}
	virtual void SetMaxDatagramSize		(DWORD dwMaxDatagramSize)			{ENSURE_HAS_STOPPED(); m_dwMaxDatagramSize		= dwMaxDatagramSize;}
	virtual void SetFreeBufferPoolSize	(DWORD dwFreeBufferPoolSize)		{ENSURE_HAS_STOPPED(); m_dwFreeBufferPoolSize	= dwFreeBufferPoolSize;}
	virtual void SetFreeBufferPoolHold	(DWORD dwFreeBufferPoolHold)		{ENSURE_HAS_STOPPED(); m_dwFreeBufferPoolHold	= dwFreeBufferPoolHold;}
	virtual void SetCastMode			(EnCastMode enCastMode)				{ENSURE_HAS_STOPPED(); m_enCastMode				= enCastMode;}
	virtual void SetMultiCastTtl		(int iMCTtl)						{ENSURE_HAS_STOPPED(); m_iMCTtl					= iMCTtl;}
	virtual void SetMultiCastLoop		(BOOL bMCLoop)						{ENSURE_HAS_STOPPED(); m_bMCLoop				= bMCLoop;}
	virtual void SetExtra				(PVOID pExtra)						{m_pExtra										= pExtra;}

	virtual EnReuseAddressPolicy GetReuseAddressPolicy	()	{return m_enReusePolicy;}
	virtual DWORD GetMaxDatagramSize	()	{return m_dwMaxDatagramSize;}
	virtual DWORD GetFreeBufferPoolSize	()	{return m_dwFreeBufferPoolSize;}
	virtual DWORD GetFreeBufferPoolHold	()	{return m_dwFreeBufferPoolHold;}
	virtual EnCastMode GetCastMode		()	{return m_enCastMode;}
	virtual int GetMultiCastTtl			()	{return m_iMCTtl;}
	virtual BOOL IsMultiCastLoop		()	{return m_bMCLoop;}
	virtual PVOID GetExtra				()	{return m_pExtra;}

	virtual BOOL GetRemoteAddress(TCHAR lpszAddress[], int& iAddressLen, USHORT& usPort)
	{
		ADDRESS_FAMILY usFamily;
		return ::sockaddr_IN_2_A(m_remoteAddr, usFamily, lpszAddress, iAddressLen, usPort);
	}

protected:
	virtual EnHandleResult FirePrepareConnect(SOCKET socket)
		{return m_pListener->OnPrepareConnect(this, m_dwConnID, socket);}
	virtual EnHandleResult FireConnect()
		{
			EnHandleResult rs		= m_pListener->OnConnect(this, m_dwConnID);
			if(rs != HR_ERROR) rs	= FireHandShake();
			return rs;
		}
	virtual EnHandleResult FireHandShake()
		{return m_pListener->OnHandShake(this, m_dwConnID);}
	virtual EnHandleResult FireSend(const BYTE* pData, int iLength)
		{return m_pListener->OnSend(this, m_dwConnID, pData, iLength);}
	virtual EnHandleResult FireReceive(const BYTE* pData, int iLength)
		{return m_pListener->OnReceive(this, m_dwConnID, pData, iLength);}
	virtual EnHandleResult FireReceive(int iLength)
		{return m_pListener->OnReceive(this, m_dwConnID, iLength);}
	virtual EnHandleResult FireClose(EnSocketOperation enOperation, int iErrorCode)
		{return m_pListener->OnClose(this, m_dwConnID, enOperation, iErrorCode);}

	void SetLastError(EnSocketError code, LPCSTR func, int ec);
	virtual BOOL CheckParams();
	virtual void PrepareStart();
	virtual void Reset();

	virtual void OnWorkerThreadStart(THR_ID dwThreadID) {}
	virtual void OnWorkerThreadEnd(THR_ID dwThreadID) {}

protected:
	void SetReserved	(PVOID pReserved)	{m_pReserved = pReserved;}
	PVOID GetReserved	()					{return m_pReserved;}
	BOOL GetRemoteHost	(LPCSTR* lpszHost, USHORT* pusPort = nullptr);

private:
	void SetRemoteHost	(LPCTSTR lpszHost, USHORT usPort);
	void SetConnected	(BOOL bConnected = TRUE) {m_bConnected = bConnected; if(bConnected) m_enState = SS_STARTED;}

	BOOL CheckStarting();
	BOOL CheckStoping(DWORD dwCurrentThreadID);
	BOOL CreateClientSocket(LPCTSTR lpszRemoteAddress, USHORT usPort, LPCTSTR lpszBindAddress, HP_SOCKADDR& bindAddr);
	BOOL BindClientSocket(HP_SOCKADDR& bindAddr);
	BOOL ConnectToGroup(const HP_SOCKADDR& bindAddr, const HP_SOCKADDR& sourceADdr);
	BOOL CreateWorkerThreads();
	BOOL ProcessNetworkEvent();
	BOOL ReadData();
	BOOL ProcessData();
	BOOL SendData();
	TItem* GetSendBuffer();
	int SendInternal(TItemPtr& itPtr);
	void WaitForWorkerThreadsEnd(DWORD dwCurrentThreadID);

	BOOL HandleError(WSANETWORKEVENTS& events);
	BOOL HandleRead(WSANETWORKEVENTS& events);
	BOOL HandleWrite(WSANETWORKEVENTS& events);
	BOOL HandleConnect(WSANETWORKEVENTS& events);
	BOOL HandleClose(WSANETWORKEVENTS& events);

    static void WaitForWorkerThreadEnd(DWORD curThreadId, HANDLE& theadHandle, UINT& threadId);
	static UINT WINAPI NetworkThreadProc(LPVOID pv);
	static UINT WINAPI ProcessorThreadProc(LPVOID pv);

public:
	CUdpCast(IUdpCastListener* pListener)
	: m_pListener			(pListener)
	, m_lsSend				(m_itPool)
	, m_lsReceive			(m_receivePool)
	, m_soClient			(INVALID_SOCKET)
	, m_evSocket			(nullptr)
	, m_dwConnID			(0)
	, m_usPort				(0)
	, m_hThreadNetwork		(nullptr)
	, m_hThreadProcessor    (nullptr)
	, m_dwNetworkWorkerID			(0)
	, m_dwProcessorWorkerID			(0)
	, m_bPaused				(FALSE)
	, m_iPending			(0)
	, m_bConnected			(FALSE)
	, m_enLastError			(SE_OK)
	, m_enState				(SS_STOPPED)
	, m_pExtra				(nullptr)
	, m_pReserved			(nullptr)
	, m_enReusePolicy		(RAP_ADDR_ONLY)
	, m_dwMaxDatagramSize	(DEFAULT_UDP_MAX_DATAGRAM_SIZE)
	, m_dwFreeBufferPoolSize(DEFAULT_CLIENT_FREE_BUFFER_POOL_SIZE)
	, m_dwFreeBufferPoolHold(DEFAULT_CLIENT_FREE_BUFFER_POOL_HOLD)
	, m_iMCTtl				(1)
	, m_bMCLoop				(FALSE)
	, m_enCastMode			(CM_MULTICAST)
	, m_castAddr			(AF_UNSPEC, TRUE)
	, m_remoteAddr			(AF_UNSPEC, TRUE)
	, m_evWait				(TRUE, TRUE)
    , m_evWorker			(TRUE)
    , m_evReceived			(FALSE)
	, m_recvCounter			(0)
	, m_bufWatermark		(0)
	{
		ASSERT(sm_wsSocket.IsValid());
		ASSERT(m_pListener);
	}

	virtual ~CUdpCast()
	{
		ENSURE_STOP();
	}

private:
	static const CInitSocket sm_wsSocket;

private:
	CEvt				m_evWait;

	IUdpCastListener*	m_pListener;
	TClientCloseContext m_ccContext;

	SOCKET				m_soClient;
	HANDLE				m_evSocket;
	CONNID				m_dwConnID;

	EnReuseAddressPolicy m_enReusePolicy;
	DWORD				m_dwMaxDatagramSize;
	DWORD				m_dwFreeBufferPoolSize;
	DWORD				m_dwFreeBufferPoolHold;

	int					m_iMCTtl;
	BOOL				m_bMCLoop;
	EnCastMode			m_enCastMode;

	HANDLE				m_hThreadNetwork;
	HANDLE				m_hThreadProcessor;
	UINT				m_dwNetworkWorkerID;
	UINT				m_dwProcessorWorkerID;

	EnSocketError		m_enLastError;
	volatile BOOL		m_bConnected;
	volatile EnServiceState	m_enState;

	PVOID				m_pExtra;
	PVOID				m_pReserved;

	HP_SOCKADDR			m_castAddr;
	HP_SOCKADDR			m_remoteAddr;

protected:
	CStringA			m_strHost;
	USHORT				m_usPort;

	CItemPool			m_itPool;

private:
	CSpinGuard			m_csState;

	CCriSec				m_csSend;
	TItemList			m_lsSend;

	CEvt				m_evBuffer;
	CEvt				m_evWorker;
	CEvt				m_evUnpause;


	CItemPool			m_receivePool;
	TItemList			m_lsReceive;
	CEvt				m_evReceived;
	CCriSec				m_csReceive;

	volatile int		m_iPending;
	volatile BOOL		m_bPaused;

	std::int32_t m_recvCounter;
	std::int32_t m_bufWatermark;
};

#endif
