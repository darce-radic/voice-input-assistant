# Deploying Voice Input Assistant to Netlify

## 🚀 Quick Deploy (Recommended)

### Option 1: Deploy via Netlify CLI
```bash
# Install Netlify CLI globally (if not already installed)
npm install -g netlify-cli

# Login to Netlify
netlify login

# Initialize and link to Netlify
netlify init

# Deploy to production
netlify deploy --prod
```

### Option 2: Deploy via GitHub Integration

1. Push your code to GitHub
2. Go to [Netlify Dashboard](https://app.netlify.com)
3. Click "New site from Git"
4. Connect to GitHub and select your repository
5. Configure build settings:
   - **Build command**: `npm run build`
   - **Publish directory**: `.next`
6. Click "Deploy site"

## 🔧 Configuration Steps

### 1. Environment Variables
In Netlify Dashboard > Site Settings > Environment Variables, add:

```env
NEXT_PUBLIC_SITE_URL=https://your-site.netlify.app
NEXT_PUBLIC_API_URL=https://api.yourdomain.com
NEXT_PUBLIC_APP_URL=https://app.yourdomain.com
```

### 2. Custom Domain (Optional)
1. Go to Domain Settings in Netlify
2. Add custom domain
3. Configure DNS:
   - Add CNAME record pointing to `your-site.netlify.app`
   - Or use Netlify DNS for automatic configuration

### 3. Form Handling
Netlify automatically detects forms. Add to any form:
```html
<form name="contact" method="POST" data-netlify="true">
```

### 4. Serverless Functions (Optional)
Create functions in `netlify/functions/`:
```javascript
// netlify/functions/hello.js
exports.handler = async (event, context) => {
  return {
    statusCode: 200,
    body: JSON.stringify({ message: "Hello from Netlify Functions!" })
  };
};
```

## 📱 Deploy Web App Dashboard

For the dashboard (`apps/web-dashboard`):

```bash
cd ../web-dashboard
netlify init
netlify deploy --prod
```

You'll get: `voice-assistant-dashboard.netlify.app`

## 🔗 Deployment URLs

After deployment, you'll have:
- **Marketing Site**: `voice-assistant.netlify.app`
- **Web Dashboard**: `voice-assistant-dashboard.netlify.app`
- **Desktop App**: Distributed via GitHub Releases or your CDN

## 🎯 Post-Deployment Checklist

- [ ] Test all pages load correctly
- [ ] Verify forms work (contact, newsletter)
- [ ] Check responsive design on mobile
- [ ] Test download links
- [ ] Verify meta tags for SEO
- [ ] Set up Google Analytics
- [ ] Configure custom domain
- [ ] Enable Netlify Analytics (optional)
- [ ] Set up Split Testing (optional)

## 🔄 Continuous Deployment

Every push to `main` branch will trigger automatic deployment.
- **Production**: `main` branch → `your-site.netlify.app`
- **Preview**: Pull requests → `deploy-preview-{number}--your-site.netlify.app`
- **Branch deploys**: Other branches → `{branch}--your-site.netlify.app`

## 📊 Monitoring

Access analytics and logs:
1. Netlify Dashboard > Analytics
2. Function logs: Netlify Dashboard > Functions
3. Deploy logs: Netlify Dashboard > Deploys

## 🆘 Troubleshooting

### Build Fails
- Check Node version matches locally
- Verify all dependencies are in package.json
- Check build logs in Netlify Dashboard

### 404 Errors
- Ensure `_redirects` or `netlify.toml` is configured
- For Next.js dynamic routes, ensure proper export

### Slow Performance
- Enable Netlify's asset optimization
- Use next/image for automatic image optimization
- Check bundle size with `npm run analyze`

## 📚 Resources

- [Netlify Docs](https://docs.netlify.com)
- [Next.js on Netlify](https://docs.netlify.com/frameworks/next-js/)
- [Netlify CLI](https://cli.netlify.com)
- [Netlify Functions](https://functions.netlify.com)