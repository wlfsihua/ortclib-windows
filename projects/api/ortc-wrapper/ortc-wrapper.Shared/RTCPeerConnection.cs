﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ortc_winrt_api;
using Log = ortc_winrt_api.Log;
using Windows.Foundation;
using ortc_wrapper.Shared.Internal;

namespace OrtcWrapper
{
    public class RTCPeerConnection
    {
        private bool _closed = false;
        private string audioSSRCLabel = null;
        private string videoSSRCLabel = null;
        private string cnameSSRC = null;
        private UInt32 ssrcId;

        //private RtcIceRole _iceRole = RtcIceRole.Controlling;
        //private RTCSessionDescription _localCapabilities;
       // private RTCSessionDescription _localCapabilitiesFinal;
        //private RTCSessionDescription _remoteCapabilities;

        private bool _installedIceEvents;

        private RTCRtpSender audioSender { get; set; }
        private RTCRtpSender videoSender { get; set; }
        private RTCRtpReceiver audioReceiver { get; set; }
        private RTCRtpReceiver videoReceiver { get; set; }
        private MediaDevice audioPlaybackDevice { get; set; }

        private MediaStream localStream { get; set; }
        private MediaStream remoteStream { get; set; }

        private RTCIceGatherOptions options { get; set; }
        private RTCIceGatherer iceGatherer { get; set; }
        private RTCIceGatherer iceGathererRTCP   { get; set; }
        private RTCDtlsTransport dtlsTransport  { get; set; }
        private RTCIceTransport iceTransport { get; set; }


        static public void SetPreferredVideoCaptureFormat(int frame_width,
                                            int frame_height, int fps)
        {

        }

        public delegate void RTCPeerConnectionIceEventDelegate(RTCPeerConnectionIceEvent evt);
        public delegate void MediaStreamEventEventDelegate(MediaStreamEvent evt);
        public delegate void RTCPeerConnectionHealthStatsDelegate(RTCPeerConnectionHealthStats stats);

        public event RTCPeerConnectionIceEventDelegate _OnIceCandidate;
        public event RTCPeerConnectionIceEventDelegate OnIceCandidate
        {
            add
            {
                _OnIceCandidate += value;
            }
            remove
            {
                _OnIceCandidate -= value;
            }
        }
        public event MediaStreamEventEventDelegate OnAddStream;
        public event MediaStreamEventEventDelegate OnRemoveStream;
        public event RTCPeerConnectionHealthStatsDelegate OnConnectionHealthStats;

        public RTCPeerConnection(RTCConfiguration configuration)
        {
            Logger.SetLogLevel(Log.Level.Trace);
            Logger.SetLogLevel(Log.Component.ZsLib, Log.Level.Trace);
            Logger.SetLogLevel(Log.Component.Services, Log.Level.Trace);
            Logger.SetLogLevel(Log.Component.ServicesHttp, Log.Level.Trace);
            Logger.SetLogLevel(Log.Component.OrtcLib, Log.Level.Insane);
            Logger.SetLogLevel("ortc_standup", Log.Level.Insane);


            //openpeer::services::ILogger::installDebuggerLogger();
            Logger.InstallTelnetLogger(59999, 60, true);

            Settings.ApplyDefaults();
            Ortc.Setup();

            //_installedIceEvents = false;

            options = new RTCIceGatherOptions();
            options.IceServers = new List<ortc_winrt_api.RTCIceServer>();

            foreach (RTCIceServer server in configuration.IceServers)
            {
                ortc_winrt_api.RTCIceServer ortcServer = new ortc_winrt_api.RTCIceServer();
                ortcServer.Urls = new List<string>();

                if (!string.IsNullOrEmpty(server.Credential))
                {
                    ortcServer.Credential = server.Credential;
                }

                if (!string.IsNullOrEmpty(server.Username))
                {
                    ortcServer.UserName = server.Username;
                }

                ortcServer.Urls.Add(server.Url);
                options.IceServers.Add(ortcServer);
            }

            PrepareGatherer();

            RTCCertificate.GenerateCertificate("").AsTask<RTCCertificate>().ContinueWith((cert) =>
            {
                //using (var @lock = new AutoLock(_lock))
                {
                    // Since the DTLS certificate is ready the RtcDtlsTransport can now be constructed.
                    var certs = new List<RTCCertificate>();
                    certs.Add(cert.Result);
                    dtlsTransport = new RTCDtlsTransport(iceTransport, certs);
                    if (_closed) dtlsTransport.Stop();
                }
            });

            sessionID = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToUniversalTime()).TotalMilliseconds;
        }

       
        /// <summary>
        /// Enable/Disable WebRTC statistics to ETW.
        /// </summary>
        public void ToggleETWStats(bool enable)
        {

        }

        /// <summary>
        /// Enable/Disable connection health statistics.
        /// When new connection health stats are available OnConnectionHealthStats
        //  event is raised.
        /// </summary>
        public void ToggleConnectionHealthStats(bool enable)
        {

        }

        /// <summary>
        /// Adds a new local <see cref="MediaStream"/> to be sent on this connection.
        /// </summary>
        /// <param name="stream"><see cref="MediaStream"/> to be added.</param>
        public void AddStream(MediaStream stream)
        {
            localStream = stream;
        }

        public void Close()
        {

        }

        public Task SetRemoteDescription(RTCSessionDescription description)
        {
            return null;
        }

        public Task<RTCSessionDescription> CreateAnswer()//async
        {
            return null;
        }

        private UInt64 sessionID { get; set; }
        private UInt16 sessionVersion { get; set; }
        
        void parseSDP(string sdp)
        {

        }
        string createSDP()
        {
            StringBuilder sb = new StringBuilder();

            Boolean containsAudio = (localStream.GetAudioTracks() != null) && localStream.GetAudioTracks().Count > 0;
            Boolean containsVideo = (localStream.GetVideoTracks() != null) && localStream.GetVideoTracks().Count > 0;

            //------------- Global lines START -------------
            //v=0
            sb.Append("v=");
            sb.Append(0);
            sb.Append("\r\n");


            //o=- 1045717134763489491 2 IN IP4 127.0.0.1
            sb.Append("o=-");
            sb.Append(sessionID);
            sb.Append(' ');
            sb.Append(sessionVersion);
            sb.Append(' ');
            sb.Append("IN");
            sb.Append(' ');
            sb.Append("IP4");
            sb.Append(' ');
            sb.Append("127.0.0.1");
            sb.Append("\r\n");

            //s=-
            sb.Append("s=");
            sb.Append("-");
            sb.Append("\r\n");

            //t=0 0
            sb.Append("t=");
            sb.Append(0);
            sb.Append(' ');
            sb.Append(0);
            sb.Append("\r\n");

            //a=group:BUNDLE audio video
            sb.Append("a=");
            sb.Append("group:BUNDLE");
            sb.Append(' ');
            if (containsAudio)
            {
                sb.Append("audio");
                sb.Append(' ');
            }
            if (containsVideo)
                sb.Append("video");

            sb.Append("\r\n");

            //a=msid-semantic: WMS stream_label_ce8753d3
            sb.Append("a=");
            sb.Append("msid-semantic:");
            sb.Append(' ');
            sb.Append("WMS");
            sb.Append(' ');
            sb.Append(localStream.Id);
            sb.Append("\r\n");
            //------------- Global lines END -------------

            //IList<MediaAudioTrack>  audioTracks = localStream.GetAudioTracks();

            List<UInt32> listOfSsrcIds = new List<UInt32>();
            if (ssrcId == 0)
            {
                ssrcId = (UInt32)Guid.NewGuid().GetHashCode();
                listOfSsrcIds.Add(ssrcId);
            }
            if (cnameSSRC == null)
                cnameSSRC = Guid.NewGuid().ToString();

            //------------- Media lines START -------------
            //m = audio 9 UDP / TLS / RTP / SAVPF 111 103 104 9 102 0 8 106 105 13 127 126
            if (containsAudio)
            {
                if (audioSSRCLabel == null)
                    audioSSRCLabel = Guid.NewGuid().ToString();
                var audioCapabilities = RTCRtpReceiver.GetCapabilities("audio");

                if (audioCapabilities != null)
                {
                    string mediaLine = SDPGenerator.GenerateMediaSDP("audio", audioCapabilities, iceGatherer, dtlsTransport, "0.0.0.0", listOfSsrcIds, cnameSSRC, audioSSRCLabel, localStream.Id);

                    if (!string.IsNullOrEmpty(mediaLine))
                        sb.Append(mediaLine);
                }
            }

            if (containsVideo)
            {
                if (videoSSRCLabel == null)
                    videoSSRCLabel = Guid.NewGuid().ToString();
                var videoCapabilities = RTCRtpReceiver.GetCapabilities("video");

                if (videoCapabilities != null)
                {
                    string mediaLine = SDPGenerator.GenerateMediaSDP("video", videoCapabilities, iceGatherer, dtlsTransport, "0.0.0.0", listOfSsrcIds, cnameSSRC, videoSSRCLabel, localStream.Id);

                    if (!string.IsNullOrEmpty(mediaLine))
                        sb.Append(mediaLine);
                }
            }
            string ret = sb.ToString();
            return ret;
            
        }
        public IAsyncOperation<RTCSessionDescription> CreateOffer()
        {
            Task<RTCSessionDescription> ret = Task.Run<RTCSessionDescription>(() =>
            {
                RTCSessionDescription sd = new RTCSessionDescription(RTCSdpType.Offer, this.createSDP());
                return sd;
            });

            return ret.AsAsyncOperation<RTCSessionDescription>();
        }
        public Task SetLocalDescription(RTCSessionDescription description) //async
        {
            Task ret = Task.Run(() =>
            {
                //TODO update modifications
            });
            return ret;
        }

        public Task AddIceCandidate(RTCIceCandidate candidate) //async
        {
            return null;
        }

        private void PrepareGatherer()
        {
            try
            {
                iceGatherer = new ortc_winrt_api.RTCIceGatherer(options);
                iceGatherer.OnICEGathererStateChanged += OnICEGathererStateChanged;
                iceGatherer.OnICEGathererLocalCandidate += this.RTCIceGatherer_onICEGathererLocalCandidate;
                iceGatherer.OnICEGathererCandidateComplete += this.RTCIceGatherer_onICEGathererCandidateComplete;
                iceGatherer.OnICEGathererLocalCandidateGone += this.RTCIceGatherer_onICEGathererLocalCandidateGone;
                iceGatherer.OnICEGathererError += this.RTCIceGatherer_onICEGathererError;

                iceGathererRTCP = iceGatherer.CreateAssociatedGatherer();
                //iceGathererRTCP.OnICEGathererStateChanged += OnICEGathererStateChanged;
                iceGathererRTCP.OnICEGathererLocalCandidate += this.RTCIceGatherer_onRTCPICEGathererLocalCandidate;
                //iceGathererRTCP.OnICEGathererCandidateComplete += this.RTCIceGatherer_onICEGathererCandidateComplete;
                //iceGathererRTCP.OnICEGathererLocalCandidateGone += this.RTCIceGatherer_onICEGathererLocalCandidateGone;
                //iceGathererRTCP.OnICEGathererError += this.RTCIceGatherer_onICEGathererError;
            }
            catch (Exception e)
            {
                return;
            }
            iceTransport = new ortc_winrt_api.RTCIceTransport(iceGatherer);
        }

        private void OnICEGathererStateChanged(RTCIceGathererStateChangeEvent evt)
        {
            if (evt.State == RTCIceGathererState.Complete)
            {
                
            }
        }

        private void RTCIceGatherer_onICEGathererLocalCandidate(RTCIceGathererCandidateEvent evt)
        {
            var iceEvent = new RTCPeerConnectionIceEvent();
            iceEvent.Candidate = Helper.ToWrapperIceCandidate(evt.Candidate, 1); //RTP component
            if (_OnIceCandidate != null)
                _OnIceCandidate(iceEvent);
        }

        private void RTCIceGatherer_onRTCPICEGathererLocalCandidate(RTCIceGathererCandidateEvent evt)
        {
            var iceEvent = new RTCPeerConnectionIceEvent();
            iceEvent.Candidate = Helper.ToWrapperIceCandidate(evt.Candidate, 2); //RTCP component
            if (_OnIceCandidate != null)
                _OnIceCandidate(iceEvent);
        }

        private void RTCIceGatherer_onICEGathererCandidateComplete(RTCIceGathererCandidateCompleteEvent evt)
        {
            int i = 0;
            i++;
        }

        private void RTCIceGatherer_onICEGathererLocalCandidateGone(RTCIceGathererCandidateEvent evt)
        {
            int i = 0;
            i++;
        }

        private void RTCIceGatherer_onICEGathererError(RTCIceGathererErrorEvent evt)
        {
            int i = 0;
            i++;
        }
    }
}
