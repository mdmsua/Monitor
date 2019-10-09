FROM haproxy:2.0.7-alpine
ADD haproxy.cfg /usr/local/etc/haproxy/haproxy.cfg
EXPOSE 9999
