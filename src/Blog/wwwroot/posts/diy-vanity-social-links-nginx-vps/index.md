```json meta
{
  "title": "DIY Vanity Social Links: How to Set Up Custom Subdomain Redirects on Your VPS with Nginx",
  "lead": "Learn how to create custom subdomain redirects for your social media profiles using Nginx on your VPS. This guide will help you set up a simple yet effective solution to manage your online presence.",
  "isPublished": true,
  "publishedAt": "2025-05-18",
  "openGraphImage": "posts/diy-vanity-social-links-nginx-vps/og-image.png"
}
```

Over the last 14 years I've been making and sharing a lot of content online across a lot of different platforms such as YouTube, Twitter, Instagram, and more. I have a lot of different social media accounts and I wanted to make it easier for people to find me online. I decided to create custom subdomain redirects for my social media profiles using Nginx on my VPS. I figure if I am interested in doing this, then maybe others are too so I thought I would share how I did it.

> [!NOTE]
> Doing stuff like this might be one of the best parts of owning your own domain, setting up your own VPS, and having your own server. You can do whatever you want with it.

## What is a Vanity URL?

A vanity URL is a custom URL that is easy to remember and type. It usually contains your name or brand and redirects to a longer, more complex URL. For example, instead of using a long URL like `https://www.example.com/user/123456789`, you could use `https://twitter.example.com` to redirect to your Twitter profile. This makes it easier for you to tell people where to find you online and makes it easier for them to remember your social media profiles. Also in my opinion it is another way to flex your skills as a tech professional. It demonstrates that you have knowledge about:

- Acquiring a domain name
- Managing DNS records
- Setting up a VPS
- Installing and configuring Nginx
- Using SSL certificates
- Using a reverse proxy

In my case my goal was to create custom subdomains for each of the platforms I want to be able to easily refer people to. For example, `linkedin.stevanfreeborn.com` would redirect to my LinkedIn profile. I also wanted to make sure that the redirects were secure and used HTTPS.

## How I Did It

The good news is that this is a pretty simple process. I am going to walk you through the steps I took to set up my own custom subdomain redirects using Nginx on my VPS. The process is pretty straightforward and can be done in a few minutes.

> [!NOTE]
> If you want to do something similar yourself, you will need to have a domain name, a VPS with [Nginx](https://nginx.org/) installed, and have the domain name pointing to the VPS.. There are a lot of resources available online to help get you started with both.

- [Setup Domain](https://support.hostinger.com/en/articles/1583227-how-to-point-a-domain-to-your-vps)
- [Initial Server Setup](https://www.digitalocean.com/community/tutorials/initial-server-setup-with-ubuntu-20-04)
- [Install Nginx](https://www.digitalocean.com/community/tutorials/how-to-install-nginx-on-ubuntu-20-04)

### Create DNS Records

- Log in to your domain registrar's control panel.
- Go to the DNS management section.
- Create a new CNAME record for each subdomain you want to create. For example, if you want to create a subdomain for your LinkedIn profile, create a CNAME record for `linkedin.your_domain.com` and point it to `your_domain.com`.
- Make sure to set the TTL (Time to Live) to a low value (like 300 seconds) so that changes propagate quickly.
- Save the changes and wait for the DNS records to propagate. This can take anywhere from a few minutes to a few hours, depending on your domain registrar.
- You can check if the DNS records have propagated by using a tool like [WhatsMyDNS](https://www.whatsmydns.net/) or by running the following command in your terminal:

```bash
nslookup linkedin.your_domain.com
```

> [!NOTE]
> Make sure to replace `linkedin.your_domain.com` with the actual subdomain you created.

### Configure Nginx

- SSH into your VPS.
- Open the Nginx configuration file for your site. This is usually located at `/etc/nginx/sites-available/default` or `/etc/nginx/sites-available/your_domain.conf`.
- Add the following server block for each subdomain you want to create:

```nginx
server {
    listen 80;
    server_name subdomain.your_domain.com;

    location / {
        return 301 https://www.example.com;
    }
}
```

> [!NOTE]
> Make sure to replace `subdomain.your_domain.com` with your actual subdomain.

- Save the file and exit the editor.
- Create a symbolic link to the configuration file in the `sites-enabled` directory if it doesn't already exist:

```bash
sudo ln -s /etc/nginx/sites-available/your_domain.conf /etc/nginx/sites-enabled/your_domain.conf
```

- Test the Nginx configuration to make sure there are no syntax errors:

```bash
sudo nginx -t
```

- If there are no errors, reload Nginx to apply the changes:

```bash
sudo nginx -s reload
```

### Install SSL Certificate

- To secure your redirects with HTTPS, you will need to install an SSL certificate. The easiest way to do this is to use [Certbot](https://certbot.eff.org/), which is a free and open-source tool for obtaining and renewing SSL certificates.
- Install Certbot on your VPS by following the instructions for your operating system on the [Certbot website](https://certbot.eff.org/instructions).
- Once Certbot is installed, run the following command to obtain an SSL certificate for your subdomain:

```bash
sudo certbot --nginx -d subdomain.your_domain.com
```

> [!NOTE]
> Make sure to replace `subdomain.your_domain.com` with the actual subdomain.

- Follow the prompts to complete the installation. Certbot will automatically configure Nginx to use the SSL certificate and redirect HTTP traffic to HTTPS.
- Test the Nginx configuration to make sure there are no syntax errors:

```bash 
sudo nginx -t
```

- If there are no errors, reload Nginx to apply the changes:

```bash
sudo nginx -s reload
```

### Test the Redirects

With everything set up, you can now test your redirects. Open a web browser and enter the subdomain URL (e.g., `https://linkedin.your_domain.com`). You should be redirected to the corresponding social media profile (e.g., `https://www.linkedin.com/in/your_profile`). Make sure to test all the subdomains you created to ensure they are working correctly.

## Conclusion

Setting up custom subdomain redirects for your social media profiles using Nginx on your VPS is a simple and effective way to manage your online presence. By following the steps outlined in this guide, you can create easy-to-remember URLs that redirect to your social media profiles while ensuring that the connections are secure with HTTPS. If you have any questions or run into any issues, feel free to reach out for help. You can find me on Bluesky at [@stevanfreeborn.com](https://bluesky.stevanfreeborn.com).
