﻿using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkyApm.Diagnostics.Grpc.Client
{
    public class ClientDiagnosticInterceptor : Interceptor
    {
        private readonly ClientDiagnosticProcessor _processor;

        public ClientDiagnosticInterceptor(ClientDiagnosticProcessor processor)
        {
            _processor = processor;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = _processor.BeginRequest(context);
            try
            {
                var options = context.Options.WithHeaders(metadata);
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
                var response = continuation(request, context);
                _processor.EndRequest();
                return response;
            }
            catch (Exception ex)
            {
                _processor.DiagnosticUnhandledException(ex);
                throw ex;
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = _processor.BeginRequest(context);
            try
            {
                var options = context.Options.WithHeaders(metadata);
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
                var response = continuation(request, context);
                var responseAsync = response.ResponseAsync.ContinueWith(r =>
                {
                    try
                    {
                        _processor.EndRequest();
                        return r.Result;
                    }
                    catch (Exception ex)
                    {
                        _processor.DiagnosticUnhandledException(ex);
                        throw ex;
                    }
                });
                return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
            }
            catch (Exception ex)
            {
                _processor.DiagnosticUnhandledException(ex);
                throw ex;
            }
        }
    }
}
