import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const requestedPath = request.nextUrl.pathname;
  
  // Set proper MIME types for static assets
  if (requestedPath.endsWith('.js')) {
    return new NextResponse(null, {
      headers: {
        'Content-Type': 'application/javascript',
      },
    });
  }
  
  if (requestedPath.endsWith('.css')) {
    return new NextResponse(null, {
      headers: {
        'Content-Type': 'text/css',
      },
    });
  }

  return NextResponse.next();
}